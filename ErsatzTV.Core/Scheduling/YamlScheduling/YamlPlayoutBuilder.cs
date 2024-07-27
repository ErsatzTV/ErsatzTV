using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutBuilder(
    ILocalFileSystem localFileSystem,
    IConfigElementRepository configElementRepository,
    IMediaCollectionRepository mediaCollectionRepository,
    ILogger<YamlPlayoutBuilder> logger)
    : IYamlPlayoutBuilder
{
    public async Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken)
    {
        if (!localFileSystem.FileExists(playout.TemplateFile))
        {
            logger.LogWarning("YAML playout file {File} does not exist; aborting.", playout.TemplateFile);
            return playout;
        }

        YamlPlayoutDefinition playoutDefinition = await LoadYamlDefinition(playout, cancellationToken);

        DateTimeOffset start = DateTimeOffset.Now;
        int daysToBuild = await GetDaysToBuild();
        DateTimeOffset finish = start.AddDays(daysToBuild);

        if (mode is not PlayoutBuildMode.Reset)
        {
            logger.LogWarning("YAML playouts can only be reset; ignoring build mode {Mode}", mode.ToString());
            return playout;
        }

        DateTimeOffset currentTime = start;

        currentTime = HandleResetActions(playout, playoutDefinition, currentTime);

        // load content and content enumerators on demand
        Dictionary<string, IMediaCollectionEnumerator> enumerators = new();
        System.Collections.Generic.HashSet<string> missingContentKeys = [];

        int itemsAfterRepeat = playout.Items.Count;
        var index = 0;
        while (currentTime < finish)
        {
            if (index >= playoutDefinition.Playout.Count)
            {
                logger.LogInformation("Reached the end of the YAML playout definition; stopping");
                break;
            }

            YamlPlayoutInstruction playoutItem = playoutDefinition.Playout[index];

            // handle instructions that don't reference content
            switch (playoutItem)
            {
                case YamlPlayoutWaitUntilInstruction waitUntil:
                    currentTime = HandleWaitUntil(currentTime, waitUntil);
                    break;
                case YamlPlayoutRepeatInstruction:
                    // repeat resets index into YAML playout
                    index = 0;
                    if (playout.Items.Count == itemsAfterRepeat)
                    {
                        logger.LogWarning("Repeat encountered without adding any playout items; aborting");
                        break;
                    }

                    itemsAfterRepeat = playout.Items.Count;
                    continue;
            }

            Option<IMediaCollectionEnumerator> maybeEnumerator = await GetCachedEnumeratorForContent(
                playout,
                playoutDefinition,
                enumerators,
                playoutItem.Content,
                cancellationToken);

            if (maybeEnumerator.IsNone)
            {
                if (!missingContentKeys.Contains(playoutItem.Content))
                {
                    logger.LogWarning("Unable to locate content with key {Key}", playoutItem.Content);
                    missingContentKeys.Add(playoutItem.Content);
                }
            }

            foreach (IMediaCollectionEnumerator enumerator in maybeEnumerator)
            {
                switch (playoutItem)
                {
                    case YamlPlayoutCountInstruction count:
                        currentTime = YamlPlayoutSchedulerCount.Schedule(playout, currentTime, count, enumerator);
                        break;
                    case YamlPlayoutDurationInstruction duration:
                        Option<IMediaCollectionEnumerator> durationFallbackEnumerator = await GetCachedEnumeratorForContent(
                            playout,
                            playoutDefinition,
                            enumerators,
                            duration.Fallback,
                            cancellationToken);
                        currentTime = YamlPlayoutSchedulerDuration.Schedule(
                            playout,
                            currentTime,
                            duration,
                            enumerator,
                            durationFallbackEnumerator);
                        break;
                    case YamlPlayoutPadToNextInstruction padToNext:
                        Option<IMediaCollectionEnumerator> fallbackEnumerator = await GetCachedEnumeratorForContent(
                            playout,
                            playoutDefinition,
                            enumerators,
                            padToNext.Fallback,
                            cancellationToken);
                        currentTime = YamlPlayoutSchedulerPadToNext.Schedule(
                            playout,
                            currentTime,
                            padToNext,
                            enumerator,
                            fallbackEnumerator);
                        break;
                }
            }

            index++;
        }

        return playout;
    }

    private DateTimeOffset HandleResetActions(
        Playout playout,
        YamlPlayoutDefinition playoutDefinition,
        DateTimeOffset currentTime)
    {
        // these are only for reset
        playout.Seed = new Random().Next();
        playout.Items.Clear();

        foreach (YamlPlayoutInstruction instruction in playoutDefinition.Reset)
        {
            switch (instruction)
            {
                case YamlPlayoutWaitUntilInstruction waitUntil:
                    currentTime = HandleWaitUntil(currentTime, waitUntil);
                    break;
                default:
                    logger.LogInformation(
                        "Skipping unsupported reset instruction {Instruction}",
                        instruction.GetType().Name);
                    break;
            }
        }

        return currentTime;
    }

    private static DateTimeOffset HandleWaitUntil(DateTimeOffset currentTime, YamlPlayoutWaitUntilInstruction waitUntil)
    {
        if (TimeOnly.TryParse(waitUntil.WaitUntil, out TimeOnly result))
        {
            var dayOnly = DateOnly.FromDateTime(currentTime.LocalDateTime);
            var timeOnly = TimeOnly.FromDateTime(currentTime.LocalDateTime);

            if (timeOnly > result)
            {
                if (waitUntil.Tomorrow)
                {
                    // this is wrong when offset changes
                    dayOnly = dayOnly.AddDays(1);
                    currentTime = new DateTimeOffset(dayOnly, result, currentTime.Offset);
                }
            }
            else
            {
                // this is wrong when offset changes
                currentTime = new DateTimeOffset(dayOnly, result, currentTime.Offset);
            }
        }

        return currentTime;
    }

    private async Task<int> GetDaysToBuild() =>
        await configElementRepository
            .GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
            .IfNoneAsync(2);

    private async Task<Option<IMediaCollectionEnumerator>> GetCachedEnumeratorForContent(
        Playout playout,
        YamlPlayoutDefinition playoutDefinition,
        Dictionary<string, IMediaCollectionEnumerator> enumerators,
        string contentKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentKey))
        {
            return Option<IMediaCollectionEnumerator>.None;
        }

        if (!enumerators.TryGetValue(contentKey, out IMediaCollectionEnumerator enumerator))
        {
            Option<IMediaCollectionEnumerator> maybeEnumerator =
                await GetEnumeratorForContent(playout, contentKey, playoutDefinition, cancellationToken);

            if (maybeEnumerator.IsNone)
            {
                return Option<IMediaCollectionEnumerator>.None;
            }

            foreach (IMediaCollectionEnumerator e in maybeEnumerator)
            {
                enumerator = e;
                enumerators.Add(contentKey, enumerator);
            }
        }

        return Some(enumerator);
    }

    private async Task<Option<IMediaCollectionEnumerator>> GetEnumeratorForContent(
        Playout playout,
        string contentKey,
        YamlPlayoutDefinition playoutDefinition,
        CancellationToken cancellationToken)
    {
        int index = playoutDefinition.Content.FindIndex(c => c.Key == contentKey);
        if (index < 0)
        {
            return Option<IMediaCollectionEnumerator>.None;
        }

        List<MediaItem> items = [];

        YamlPlayoutContentItem content = playoutDefinition.Content[index];
        switch (content)
        {
            case YamlPlayoutContentSearchItem search:
                items = await mediaCollectionRepository.GetSmartCollectionItems(search.Query);
                break;
            case YamlPlayoutContentShowItem show:
                items = await mediaCollectionRepository.GetShowItemsByShowGuids(
                    show.Guids.Map(g => $"{g.Source}://{g.Value}").ToList());
                break;
        }

        var state = new CollectionEnumeratorState { Seed = playout.Seed + index, Index = 0 };
        switch (Enum.Parse<PlaybackOrder>(content.Order, true))
        {
            case PlaybackOrder.Chronological:
                return new ChronologicalMediaCollectionEnumerator(items, state);
            case PlaybackOrder.Shuffle:
                // TODO: fix this
                var groupedMediaItems = items.Map(mi => new GroupedMediaItem(mi, null)).ToList();
                return new ShuffledMediaCollectionEnumerator(groupedMediaItems, state, cancellationToken);
        }

        return Option<IMediaCollectionEnumerator>.None;
    }

    private static async Task<YamlPlayoutDefinition> LoadYamlDefinition(Playout playout, CancellationToken cancellationToken)
    {
        string yaml = await File.ReadAllTextAsync(playout.TemplateFile, cancellationToken);

        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeDiscriminatingNodeDeserializer(
                o =>
                {
                    var contentKeyMappings = new Dictionary<string, Type>
                    {
                        { "search", typeof(YamlPlayoutContentSearchItem) },
                        { "show", typeof(YamlPlayoutContentShowItem) }
                    };

                    o.AddUniqueKeyTypeDiscriminator<YamlPlayoutContentItem>(contentKeyMappings);

                    var instructionKeyMappings = new Dictionary<string, Type>
                    {
                        { "count", typeof(YamlPlayoutCountInstruction) },
                        { "duration", typeof(YamlPlayoutDurationInstruction) },
                        { "pad_to_next", typeof(YamlPlayoutPadToNextInstruction) },
                        { "repeat", typeof(YamlPlayoutRepeatInstruction) },
                        { "wait_until", typeof(YamlPlayoutWaitUntilInstruction) }
                    };

                    o.AddUniqueKeyTypeDiscriminator<YamlPlayoutInstruction>(instructionKeyMappings);
                })
            .Build();

        return deserializer.Deserialize<YamlPlayoutDefinition>(yaml);
    }
}
