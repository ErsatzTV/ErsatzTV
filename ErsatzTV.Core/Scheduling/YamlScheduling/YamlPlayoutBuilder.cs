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

        // load content and content enumerators on demand
        Dictionary<YamlPlayoutInstruction, IYamlPlayoutHandler> handlers = new();
        Dictionary<string, IMediaCollectionEnumerator> enumerators = new();
        System.Collections.Generic.HashSet<string> missingContentKeys = [];

        var context = new YamlPlayoutContext(playout)
        {
            CurrentTime = start,
            GuideGroup = 1,
            InstructionIndex = 0
        };

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (mode is PlayoutBuildMode.Reset)
        {
            context.Playout.Seed = new Random().Next();
            context.Playout.Items.Clear();

            foreach (YamlPlayoutInstruction instruction in playoutDefinition.Reset)
            {
                Option<IYamlPlayoutHandler> maybeHandler = GetHandlerForInstruction(handlers, instruction);
                foreach (IYamlPlayoutHandler handler in maybeHandler)
                {
                    if (!handler.Reset)
                    {
                        logger.LogInformation(
                            "Skipping unsupported reset instruction {Instruction}",
                            instruction.GetType().Name);
                    }
                    else
                    {
                        handler.Handle(context, instruction, logger);
                    }
                }
            }
        }

        while (context.CurrentTime < finish)
        {
            if (context.InstructionIndex >= playoutDefinition.Playout.Count)
            {
                logger.LogInformation("Reached the end of the YAML playout definition; stopping");
                break;
            }

            YamlPlayoutInstruction instruction = playoutDefinition.Playout[context.InstructionIndex];
            Option<IYamlPlayoutHandler> maybeHandler = GetHandlerForInstruction(handlers, instruction);

            var handled = false;
            foreach (IYamlPlayoutHandler handler in maybeHandler)
            {
                if (!handler.Handle(context, instruction, logger))
                {
                    logger.LogInformation("YAML playout instruction handler failed");
                }

                handled = true;
            }

            if (handled)
            {
                continue;
            }

            Option<IMediaCollectionEnumerator> maybeEnumerator = await GetCachedEnumeratorForContent(
                context,
                playout,
                playoutDefinition,
                enumerators,
                instruction.Content,
                cancellationToken);

            if (maybeEnumerator.IsNone)
            {
                if (!missingContentKeys.Contains(instruction.Content))
                {
                    logger.LogWarning("Unable to locate content with key {Key}", instruction.Content);
                    missingContentKeys.Add(instruction.Content);
                }
            }

            foreach (IMediaCollectionEnumerator enumerator in maybeEnumerator)
            {
                switch (instruction)
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

    private static Option<IYamlPlayoutHandler> GetHandlerForInstruction(
        Dictionary<YamlPlayoutInstruction, IYamlPlayoutHandler> handlers,
        YamlPlayoutInstruction instruction)
    {
        if (handlers.TryGetValue(instruction, out IYamlPlayoutHandler handler))
        {
            return Optional(handler);
        }

        handler = instruction switch
        {
            YamlPlayoutRepeatInstruction => new YamlPlayoutRepeatHandler(),
            YamlPlayoutWaitUntilInstruction => new YamlPlayoutWaitUntilHandler(),
            YamlPlayoutNewEpgGroupInstruction => new YamlPlayoutNewEpgGroupHandler(),
            YamlPlayoutSkipItemsInstruction => new YamlPlayoutSkipItemsHandler(),
            _ => null
        };

        if (handler != null)
        {
            handlers.Add(instruction, handler);
        }

        return Optional(handler);
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
