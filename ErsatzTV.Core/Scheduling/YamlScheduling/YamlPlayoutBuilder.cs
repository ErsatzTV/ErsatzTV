using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
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

        YamlPlayoutContext context = HandleResetActions(playout, playoutDefinition, start);

        // load content and content enumerators on demand
        Dictionary<YamlPlayoutInstruction, IYamlPlayoutHandler> handlers = new();
        Dictionary<string, IMediaCollectionEnumerator> enumerators = new();
        System.Collections.Generic.HashSet<string> missingContentKeys = [];

        context.GuideGroup = 1;
        context.InstructionIndex = 0;

        while (context.CurrentTime < finish)
        {
            if (context.InstructionIndex >= playoutDefinition.Playout.Count)
            {
                logger.LogInformation("Reached the end of the YAML playout definition; stopping");
                break;
            }

            YamlPlayoutInstruction playoutItem = playoutDefinition.Playout[context.InstructionIndex];
            if (!handlers.TryGetValue(playoutItem, out IYamlPlayoutHandler handler))
            {
                handler = playoutItem switch
                {
                    YamlPlayoutRepeatInstruction => new YamlPlayoutRepeatHandler(),
                    YamlPlayoutWaitUntilInstruction => new YamlPlayoutWaitUntilHandler(),
                    YamlPlayoutNewEpgGroupInstruction => new YamlPlayoutNewEpgGroupHandler(),
                    _ => null
                };

                if (handler != null)
                {
                    handlers.Add(playoutItem, handler);
                }
            }

            if (handler != null)
            {
                if (!handler.Handle(context, playoutItem, logger))
                {
                    logger.LogInformation("YAML playout instruction handler failed");
                    break;
                }

                continue;
            }

            Option<IMediaCollectionEnumerator> maybeEnumerator = await GetCachedEnumeratorForContent(
                context,
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
                        context.CurrentTime = YamlPlayoutSchedulerCount.Schedule(context, count, enumerator);
                        break;
                    case YamlPlayoutDurationInstruction duration:
                        Option<IMediaCollectionEnumerator> durationFallbackEnumerator = await GetCachedEnumeratorForContent(
                            context,
                            playout,
                            playoutDefinition,
                            enumerators,
                            duration.Fallback,
                            cancellationToken);
                        context.CurrentTime = YamlPlayoutSchedulerDuration.Schedule(
                            context,
                            duration,
                            enumerator,
                            durationFallbackEnumerator);
                        break;
                    case YamlPlayoutPadToNextInstruction padToNext:
                        Option<IMediaCollectionEnumerator> fallbackEnumerator = await GetCachedEnumeratorForContent(
                            context,
                            playout,
                            playoutDefinition,
                            enumerators,
                            padToNext.Fallback,
                            cancellationToken);
                        context.CurrentTime = YamlPlayoutSchedulerPadToNext.Schedule(
                            context,
                            padToNext,
                            enumerator,
                            fallbackEnumerator);
                        break;
                }
            }

            context.InstructionIndex++;
        }

        return playout;
    }

    private YamlPlayoutContext HandleResetActions(
        Playout playout,
        YamlPlayoutDefinition playoutDefinition,
        DateTimeOffset currentTime)
    {
        var result = new YamlPlayoutContext(playout) { CurrentTime = currentTime };

        // these are only for reset
        playout.Seed = new Random().Next();
        playout.Items.Clear();

        foreach (YamlPlayoutInstruction instruction in playoutDefinition.Reset)
        {
            switch (instruction)
            {
                case YamlPlayoutWaitUntilInstruction waitUntil:
                    new YamlPlayoutWaitUntilHandler().Handle(result, waitUntil, logger);
                    break;
                case YamlPlayoutSkipItemsInstruction skipItems:
                    if (result.ContentIndex.TryGetValue(skipItems.Content, out int value))
                    {
                        value += skipItems.SkipItems;
                    }
                    else
                    {
                        value = skipItems.SkipItems;
                    }

                    result.ContentIndex[skipItems.Content] = value;
                    break;
                default:
                    logger.LogInformation(
                        "Skipping unsupported reset instruction {Instruction}",
                        instruction.GetType().Name);
                    break;
            }
        }

        return result;
    }

    private async Task<int> GetDaysToBuild() =>
        await configElementRepository
            .GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
            .IfNoneAsync(2);

    private async Task<Option<IMediaCollectionEnumerator>> GetCachedEnumeratorForContent(
        YamlPlayoutContext context,
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
                await GetEnumeratorForContent(context, playout, contentKey, playoutDefinition, cancellationToken);

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
        YamlPlayoutContext context,
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

        // start at the appropriate place in the enumerator
        context.ContentIndex.TryGetValue(contentKey, out int enumeratorIndex);

        var state = new CollectionEnumeratorState { Seed = playout.Seed + index, Index = enumeratorIndex };
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
                        { "new_epg_group", typeof(YamlPlayoutNewEpgGroupInstruction) },
                        { "pad_to_next", typeof(YamlPlayoutPadToNextInstruction) },
                        { "repeat", typeof(YamlPlayoutRepeatInstruction) },
                        { "skip_items", typeof(YamlPlayoutSkipItemsInstruction) },
                        { "wait_until", typeof(YamlPlayoutWaitUntilInstruction) }
                    };

                    o.AddUniqueKeyTypeDiscriminator<YamlPlayoutInstruction>(instructionKeyMappings);
                })
            .Build();

        return deserializer.Deserialize<YamlPlayoutDefinition>(yaml);
    }
}
