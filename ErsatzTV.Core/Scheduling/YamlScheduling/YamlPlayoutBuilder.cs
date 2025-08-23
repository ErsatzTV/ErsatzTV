using System.Collections.Immutable;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using ErsatzTV.Core.Search;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutBuilder(
    ILocalFileSystem localFileSystem,
    IConfigElementRepository configElementRepository,
    IMediaCollectionRepository mediaCollectionRepository,
    IChannelRepository channelRepository,
    IGraphicsElementRepository graphicsElementRepository,
    ISequentialScheduleValidator sequentialScheduleValidator,
    ILogger<YamlPlayoutBuilder> logger)
    : IYamlPlayoutBuilder
{
    public async Task<PlayoutBuildResult> Build(
        DateTimeOffset start,
        Playout playout,
        PlayoutReferenceData referenceData,
        PlayoutBuildMode mode,
        CancellationToken cancellationToken)
    {
        PlayoutBuildResult result = PlayoutBuildResult.Empty;

        if (!localFileSystem.FileExists(playout.TemplateFile))
        {
            logger.LogWarning("YAML playout file {File} does not exist; aborting.", playout.TemplateFile);
            return result;
        }

        Option<YamlPlayoutDefinition> maybePlayoutDefinition =
            await LoadYamlDefinition(playout.TemplateFile, false, cancellationToken);
        if (maybePlayoutDefinition.IsNone)
        {
            logger.LogWarning("YAML playout file {File} is invalid; aborting.", playout.TemplateFile);
            return result;
        }

        // using ValueUnsafe to avoid nesting
        YamlPlayoutDefinition playoutDefinition = maybePlayoutDefinition.ValueUnsafe();

        foreach (string import in playoutDefinition.Import)
        {
            try
            {
                string path = import;
                if (!File.Exists(import))
                {
                    path = Path.Combine(
                        Path.GetDirectoryName(playout.TemplateFile) ?? string.Empty,
                        import ?? string.Empty);
                    if (!File.Exists(path))
                    {
                        logger.LogError("YAML playout import {File} does not exist.", path);
                        return result;
                    }
                }

                Option<YamlPlayoutDefinition> maybeImportedDefinition =
                    await LoadYamlDefinition(path, true, cancellationToken);
                foreach (YamlPlayoutDefinition importedDefinition in maybeImportedDefinition)
                {
                    IEnumerable<YamlPlayoutContentItem> contentToAdd = importedDefinition.Content
                        .Where(c => playoutDefinition.Content.All(c2 => !string.Equals(
                            c2.Key,
                            c.Key,
                            StringComparison.OrdinalIgnoreCase)));

                    playoutDefinition.Content.AddRange(contentToAdd);

                    IEnumerable<YamlPlayoutSequenceItem> sequencesToAdd = importedDefinition.Sequence
                        .Where(s => playoutDefinition.Sequence.All(s2 => !string.Equals(
                            s2.Key,
                            s.Key,
                            StringComparison.OrdinalIgnoreCase)));

                    playoutDefinition.Sequence.AddRange(sequencesToAdd);
                }

                if (maybeImportedDefinition.IsNone)
                {
                    logger.LogWarning("YAML playout import {File} is invalid; aborting.", import);
                    return result;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected exception loading YAML playout import");
            }
        }

        int daysToBuild = await GetDaysToBuild();
        DateTimeOffset finish = start.AddDays(daysToBuild);

        Dictionary<YamlPlayoutInstruction, IYamlPlayoutHandler> handlers = new();
        var enumeratorCache = new EnumeratorCache(mediaCollectionRepository, logger);

        var context = new YamlPlayoutContext(playout, playoutDefinition, 1)
        {
            CurrentTime = start

            // no need to init default value and throw off visited count
            // InstructionIndex = 0
        };

        // logger.LogDebug(
        //     "Default yaml context from {Start} to {Finish}, instruction {Instruction}",
        //     context.CurrentTime,
        //     finish,
        //     context.InstructionIndex);

        // remove old items
        // importantly, this should not remove their history
        result = result with { RemoveBefore = start };

        // load saved state
        if (mode is not PlayoutBuildMode.Reset)
        {
            foreach (PlayoutAnchor prevAnchor in Optional(playout.Anchor))
            {
                context.Reset(prevAnchor, start);
            }
        }
        else
        {
            // reset (remove items and "currently active" history)

            // for testing
            // start = start.AddHours(-2);

            // erase items, not history
            result = result with { ClearItems = true };

            // remove any future or "currently active" history items
            // this prevents "walking" the playout forward by repeatedly resetting
            var toRemove = new List<PlayoutHistory>();
            toRemove.AddRange(
                referenceData.PlayoutHistory.Filter(h =>
                    h.When > start.UtcDateTime || h.When <= start.UtcDateTime && h.Finish >= start.UtcDateTime));
            foreach (PlayoutHistory history in toRemove)
            {
                result.HistoryToRemove.Add(history.Id);
            }
        }

        // logger.LogDebug(
        //     "Saved yaml context from {Start} to {Finish}, instruction {Instruction}",
        //     context.CurrentTime,
        //     finish,
        //     context.InstructionIndex);

        // apply all (filtered) history
        var filteredHistory = referenceData.PlayoutHistory.ToList();
        filteredHistory.RemoveAll(h => result.HistoryToRemove.Contains(h.Id));
        var applyHistoryHandler = new YamlPlayoutApplyHistoryHandler(enumeratorCache);
        foreach (YamlPlayoutContentItem contentItem in playoutDefinition.Content)
        {
            await applyHistoryHandler.Handle(
                filteredHistory.ToImmutableList(),
                context,
                contentItem,
                logger,
                cancellationToken);
        }

        if (mode is PlayoutBuildMode.Reset)
        {
            // handle all on-reset instructions
            foreach (YamlPlayoutInstruction instruction in playoutDefinition.Reset)
            {
                Option<IYamlPlayoutHandler> maybeHandler = GetHandlerForInstruction(
                    handlers,
                    enumeratorCache,
                    instruction);

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
                        await handler.Handle(
                            context,
                            instruction,
                            mode,
                            _ => Task.CompletedTask,
                            logger,
                            cancellationToken);
                    }
                }
            }
        }

        if (DetectCycle(context.Definition))
        {
            logger.LogError("YAML sequence contains a cycle; unable to build playout");
            return result;
        }

        var flattenCount = 0;
        while (context.Definition.Playout.Any(x => x is YamlPlayoutSequenceInstruction))
        {
            if (flattenCount > 100)
            {
                logger.LogError(
                    "YAML playout definition contains sequence nesting that is too deep; this introduces undefined behavior");
                break;
            }

            FlattenSequences(context);
            flattenCount++;
        }

        // handle all playout instructions
        while (context.CurrentTime < finish)
        {
            if (context.InstructionIndex >= playoutDefinition.Playout.Count)
            {
                logger.LogInformation("Reached the end of the YAML playout definition; stopping");
                break;
            }

            YamlPlayoutInstruction instruction = playoutDefinition.Playout[context.InstructionIndex];
            //logger.LogDebug("Current playout instruction: {Instruction}", instruction.GetType().Name);

            Option<IYamlPlayoutHandler> maybeHandler = GetHandlerForInstruction(handlers, enumeratorCache, instruction);

            foreach (IYamlPlayoutHandler handler in maybeHandler)
            {
                if (!await handler.Handle(context, instruction, mode, ExecuteSequenceLocal, logger, cancellationToken))
                {
                    logger.LogInformation("YAML playout instruction handler failed");
                }

                continue;

                async Task ExecuteSequenceLocal(string sequence)
                {
                    await ExecuteSequence(
                        handlers,
                        enumeratorCache,
                        mode,
                        context,
                        sequence,
                        cancellationToken);
                }
            }

            if (!instruction.ChangesIndex)
            {
                //logger.LogDebug("Moving to next instruction");
                context.InstructionIndex++;
            }
        }

        result = CleanUpHistory(referenceData, start, result);

        DateTime maxTime = context.CurrentTime.UtcDateTime;
        if (context.AddedItems.Count > 0)
        {
            maxTime = context.AddedItems.Max(i => i.Finish);
        }

        var anchor = new PlayoutAnchor
        {
            NextStart = maxTime,
            Context = context.Serialize()
        };

        context.AdvanceGuideGroup();

        // logger.LogDebug(
        //     "Saving yaml context at {Start}, instruction {Instruction}",
        //     maxTime,
        //     context.InstructionIndex);

        playout.Anchor = anchor;

        result.AddedItems.AddRange(context.AddedItems);
        result.AddedHistory.AddRange(context.AddedHistory);

        return result;
    }

    private async Task ExecuteSequence(
        Dictionary<YamlPlayoutInstruction, IYamlPlayoutHandler> handlers,
        EnumeratorCache enumeratorCache,
        PlayoutBuildMode mode,
        YamlPlayoutContext context,
        string sequence,
        CancellationToken cancellationToken)
    {
        var sequenceInstructions = context.Definition.Sequence
            .Filter(s => s.Key == sequence)
            .HeadOrNone()
            .Map(s => s.Items)
            .Flatten()
            .ToList();

        foreach (YamlPlayoutInstruction instruction in sequenceInstructions)
        {
            //logger.LogDebug("Current playout instruction: {Instruction}", instruction.GetType().Name);

            Option<IYamlPlayoutHandler> maybeHandler = GetHandlerForInstruction(handlers, enumeratorCache, instruction);

            foreach (IYamlPlayoutHandler handler in maybeHandler)
            {
                if (!await handler.Handle(
                        context,
                        instruction,
                        mode,
                        _ => Task.CompletedTask,
                        logger,
                        cancellationToken))
                {
                    logger.LogInformation("YAML playout instruction handler failed");
                }
            }
        }
    }

    private static bool DetectCycle(YamlPlayoutDefinition definition)
    {
        var graph = new AdjGraph();

        foreach (YamlPlayoutSequenceItem sequence in definition.Sequence)
        {
            foreach (YamlPlayoutSequenceInstruction instruction in
                     sequence.Items.OfType<YamlPlayoutSequenceInstruction>())
            {
                graph.AddEdge(sequence.Key, instruction.Sequence);
            }
        }

        return graph.HasAnyCycle();
    }

    private async Task<int> GetDaysToBuild() =>
        await configElementRepository
            .GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
            .IfNoneAsync(2);

    private static void FlattenSequences(YamlPlayoutContext context)
    {
        var rawInstructions = context.Definition.Playout.ToImmutableList();
        context.Definition.Playout.Clear();

        foreach (YamlPlayoutInstruction instruction in rawInstructions)
        {
            switch (instruction)
            {
                case YamlPlayoutSequenceInstruction sequenceInstruction:
                    IEnumerable<YamlPlayoutInstruction> sequenceInstructions = context.Definition.Sequence
                        .Filter(s => s.Key == sequenceInstruction.Sequence)
                        .HeadOrNone()
                        .Map(s => s.Items)
                        .Flatten()
                        .ToList();

                    var sequenceGuid = Guid.NewGuid();
                    int repeat = sequenceInstruction.Repeat > 0 ? sequenceInstruction.Repeat : 1;

                    for (var r = 0; r < repeat; r++)
                    {
                        // insert all instructions from the sequence
                        foreach (YamlPlayoutInstruction i in sequenceInstructions)
                        {
                            // used for shuffling
                            i.SequenceKey = sequenceInstruction.Sequence;
                            i.SequenceGuid = sequenceGuid;

                            // copy custom title
                            if (!string.IsNullOrWhiteSpace(sequenceInstruction.CustomTitle))
                            {
                                i.CustomTitle = sequenceInstruction.CustomTitle;
                            }

                            context.Definition.Playout.Add(i);
                        }
                    }

                    break;
                default:
                    context.Definition.Playout.Add(instruction);
                    break;
            }
        }
    }

    private Option<IYamlPlayoutHandler> GetHandlerForInstruction(
        Dictionary<YamlPlayoutInstruction, IYamlPlayoutHandler> handlers,
        EnumeratorCache enumeratorCache,
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
            YamlPlayoutEpgGroupInstruction => new YamlPlayoutEpgGroupHandler(),
            YamlPlayoutWatermarkInstruction => new YamlPlayoutWatermarkHandler(channelRepository),
            YamlPlayoutShuffleSequenceInstruction => new YamlPlayoutShuffleSequenceHandler(),
            YamlPlayoutPreRollInstruction => new YamlPlayoutPreRollHandler(),
            YamlPlayoutPostRollInstruction => new YamlPlayoutPostRollHandler(),
            YamlPlayoutMidRollInstruction => new YamlPlayoutMidRollHandler(),
            YamlPlayoutGraphicsOnInstruction => new YamlPlayoutGraphicsOnHandler(graphicsElementRepository),
            YamlPlayoutGraphicsOffInstruction => new YamlPlayoutGraphicsOffHandler(graphicsElementRepository),

            YamlPlayoutSkipItemsInstruction => new YamlPlayoutSkipItemsHandler(enumeratorCache),
            YamlPlayoutSkipToItemInstruction => new YamlPlayoutSkipToItemHandler(enumeratorCache),
            YamlPlayoutRewindInstruction => new YamlPlayoutRewindHandler(),

            // content handlers
            YamlPlayoutAllInstruction => new YamlPlayoutAllHandler(enumeratorCache),
            YamlPlayoutCountInstruction => new YamlPlayoutCountHandler(enumeratorCache),
            YamlPlayoutDurationInstruction => new YamlPlayoutDurationHandler(enumeratorCache),
            YamlPlayoutPadToNextInstruction => new YamlPlayoutPadToNextHandler(enumeratorCache),
            YamlPlayoutPadUntilInstruction => new YamlPlayoutPadUntilHandler(enumeratorCache),

            _ => null
        };

        if (handler != null)
        {
            handlers.Add(instruction, handler);
        }

        return Optional(handler);
    }

    private async Task<Option<YamlPlayoutDefinition>> LoadYamlDefinition(
        string fileName,
        bool isImport,
        CancellationToken cancellationToken)
    {
        try
        {
            string yaml = await File.ReadAllTextAsync(fileName, cancellationToken);
            if (!await sequentialScheduleValidator.ValidateSchedule(yaml, isImport))
            {
                return Option<YamlPlayoutDefinition>.None;
            }

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeDiscriminatingNodeDeserializer(o =>
                {
                    var contentKeyMappings = new Dictionary<string, Type>
                    {
                        { "collection", typeof(YamlPlayoutContentCollectionItem) },
                        { "marathon", typeof(YamlPlayoutContentMarathonItem) },
                        { "multi_collection", typeof(YamlPlayoutContentMultiCollectionItem) },
                        { "playlist", typeof(YamlPlayoutContentPlaylistItem) },
                        { "search", typeof(YamlPlayoutContentSearchItem) },
                        { "show", typeof(YamlPlayoutContentShowItem) },
                        { "smart_collection", typeof(YamlPlayoutContentSmartCollectionItem) }
                    };

                    o.AddUniqueKeyTypeDiscriminator<YamlPlayoutContentItem>(contentKeyMappings);

                    var instructionKeyMappings = new Dictionary<string, Type>
                    {
                        { "all", typeof(YamlPlayoutAllInstruction) },
                        { "count", typeof(YamlPlayoutCountInstruction) },
                        { "duration", typeof(YamlPlayoutDurationInstruction) },
                        { "epg_group", typeof(YamlPlayoutEpgGroupInstruction) },
                        { "graphics_on", typeof(YamlPlayoutGraphicsOnInstruction) },
                        { "graphics_off", typeof(YamlPlayoutGraphicsOffInstruction) },
                        { "watermark", typeof(YamlPlayoutWatermarkInstruction) },
                        { "pad_to_next", typeof(YamlPlayoutPadToNextInstruction) },
                        { "pad_until", typeof(YamlPlayoutPadUntilInstruction) },
                        { "pre_roll", typeof(YamlPlayoutPreRollInstruction) },
                        { "post_roll", typeof(YamlPlayoutPostRollInstruction) },
                        { "mid_roll", typeof(YamlPlayoutMidRollInstruction) },
                        { "repeat", typeof(YamlPlayoutRepeatInstruction) },
                        { "rewind", typeof(YamlPlayoutRewindInstruction) },
                        { "sequence", typeof(YamlPlayoutSequenceInstruction) },
                        { "shuffle_sequence", typeof(YamlPlayoutShuffleSequenceInstruction) },
                        { "skip_items", typeof(YamlPlayoutSkipItemsInstruction) },
                        { "skip_to_item", typeof(YamlPlayoutSkipToItemInstruction) },
                        { "wait_until", typeof(YamlPlayoutWaitUntilInstruction) }
                    };

                    o.AddUniqueKeyTypeDiscriminator<YamlPlayoutInstruction>(instructionKeyMappings);
                })
                .Build();

            return deserializer.Deserialize<YamlPlayoutDefinition>(yaml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error loading YAML playout definition");
            throw;
        }
    }

    private static PlayoutBuildResult CleanUpHistory(
        PlayoutReferenceData referenceData,
        DateTimeOffset start,
        PlayoutBuildResult result)
    {
        var groups = new Dictionary<string, List<PlayoutHistory>>();
        foreach (PlayoutHistory history in referenceData.PlayoutHistory)
        {
            if (!groups.TryGetValue(history.Key, out List<PlayoutHistory> group))
            {
                group = [];
                groups[history.Key] = group;
            }

            group.Add(history);
        }

        foreach ((string _, List<PlayoutHistory> group) in groups)
        {
            //logger.LogDebug("History key {Key} has {Count} items in group", key, group.Count);
            Option<DateTime> whenToKeep = group
                .Filter(h => h.Finish < start.UtcDateTime)
                .OrderByDescending(h => h.When)
                .Map(h => h.When)
                .HeadOrNone();

            foreach (DateTime toKeep in whenToKeep)
            {
                // keep the most recent item from each history group that has already been played completely
                IEnumerable<PlayoutHistory> toDelete = group
                    .Filter(h => h.Finish < start.UtcDateTime)
                    .Filter(h => h.When != toKeep);

                foreach (PlayoutHistory delete in toDelete)
                {
                    result.HistoryToRemove.Add(delete.Id);
                }
            }
        }

        return result;
    }
}
