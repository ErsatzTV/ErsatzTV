using System.Collections.Immutable;
using System.Globalization;
using System.IO.Abstractions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
//using ErsatzTV.Core.Scheduling.Engine;
using ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using ErsatzTV.Core.Search;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class SequentialPlayoutBuilder(
    //ISchedulingEngine schedulingEngine,
    IFileSystem fileSystem,
    IConfigElementRepository configElementRepository,
    IMediaCollectionRepository mediaCollectionRepository,
    IChannelRepository channelRepository,
    IGraphicsElementRepository graphicsElementRepository,
    ISequentialScheduleValidator sequentialScheduleValidator,
    ILogger<SequentialPlayoutBuilder> logger)
    : ISequentialPlayoutBuilder
{
    public async Task<Either<BaseError, PlayoutBuildResult>> Build(
        DateTimeOffset start,
        Playout playout,
        PlayoutReferenceData referenceData,
        PlayoutBuildMode mode,
        CancellationToken cancellationToken)
    {
        //schedulingEngine.WithMode(mode);
        //schedulingEngine.WithSeed(playout.Seed);

        PlayoutBuildResult result = PlayoutBuildResult.Empty;

        if (!fileSystem.File.Exists(playout.ScheduleFile))
        {
            logger.LogWarning("Sequential schedule file {File} does not exist; aborting.", playout.ScheduleFile);
            return BaseError.New($"Sequential schedule file {playout.ScheduleFile} does not exist");
        }

        Option<YamlPlayoutDefinition> maybePlayoutDefinition =
            await LoadYamlDefinition(playout.ScheduleFile, false, cancellationToken);
        if (maybePlayoutDefinition.IsNone)
        {
            logger.LogWarning("Sequential schedule file {File} is invalid; aborting.", playout.ScheduleFile);
            return BaseError.New($"Sequential schedule file {playout.ScheduleFile} is invalid");
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
                        Path.GetDirectoryName(playout.ScheduleFile) ?? string.Empty,
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
                    return BaseError.New($"YAML playout import {import} is invalid");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected exception loading YAML playout import");
                return BaseError.New($"Unexpected exception loading YAML playout import: {ex}");
            }
        }

        int daysToBuild = await GetDaysToBuild(cancellationToken);
        DateTimeOffset finish = start.AddDays(daysToBuild);

        Dictionary<YamlPlayoutInstruction, IYamlPlayoutHandler> handlers = new();
        var enumeratorCache = new EnumeratorCache(mediaCollectionRepository, logger);

        var context = new YamlPlayoutContext(playout, playoutDefinition, 1)
        {
            CurrentTime = start

            // no need to init default value and throw off visited count
            // InstructionIndex = 0
        };

        //schedulingEngine.BuildBetween(start, finish);

        // logger.LogDebug(
        //     "Default yaml context from {Start} to {Finish}, instruction {Instruction}",
        //     context.CurrentTime,
        //     finish,
        //     context.InstructionIndex);

        // remove old items
        // importantly, this should not remove their history
        result = result with { RemoveBefore = start };
        //schedulingEngine.RemoveBefore(start);

        //schedulingEngine.RestoreOrReset(Optional(playout.Anchor));

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
            // if (!Enum.TryParse(contentItem.Order, true, out PlaybackOrder playbackOrder))
            // {
            //     continue;
            // }
            //
            // switch (contentItem)
            // {
            //     case YamlPlayoutContentCollectionItem collectionItem:
            //         // also applies history
            //         await schedulingEngine.AddCollection(
            //             collectionItem.Key,
            //             collectionItem.Collection,
            //             playbackOrder);
            //         break;
            // }

            await applyHistoryHandler.Handle(
                filteredHistory.ToImmutableList(),
                context,
                contentItem,
                logger,
                cancellationToken);
        }

        // Determine which schedule to use based on the start date
        YamlPlayoutSchedule activeSchedule = GetActiveSchedule(playoutDefinition, start);
        List<YamlPlayoutInstruction> resetInstructions;
        List<YamlPlayoutInstruction> playoutInstructions;

        if (activeSchedule != null)
        {
            logger.LogInformation(
                "Using scheduled playout '{Name}' for date {Date}",
                string.IsNullOrWhiteSpace(activeSchedule.Name) ? "Unnamed" : activeSchedule.Name,
                start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            // Use schedule-specific reset if provided, otherwise use root reset
            resetInstructions = activeSchedule.Reset.Count > 0 ? activeSchedule.Reset : playoutDefinition.Reset;
            playoutInstructions = activeSchedule.Playout;
        }
        else
        {
            logger.LogDebug("Using default playout for date {Date}", start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            resetInstructions = playoutDefinition.Reset;
            playoutInstructions = playoutDefinition.Playout;
        }

        if (playoutInstructions.Count == 0)
        {
            logger.LogWarning("No playout instructions found for date {Date}", start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            return BaseError.New($"No playout instructions found for date {start:yyyy-MM-dd}");
        }

        if (mode is PlayoutBuildMode.Reset)
        {
            // handle all on-reset instructions
            foreach (YamlPlayoutInstruction instruction in resetInstructions)
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

        // Create a temporary modified definition with the selected instructions
        var effectiveDefinition = new YamlPlayoutDefinition
        {
            Import = playoutDefinition.Import,
            Content = playoutDefinition.Content,
            Sequence = playoutDefinition.Sequence,
            Reset = resetInstructions,
            Playout = playoutInstructions
        };

        // Update context to use the effective definition
        context = new YamlPlayoutContext(playout, effectiveDefinition, 1)
        {
            CurrentTime = context.CurrentTime,
            InstructionIndex = context.InstructionIndex
        };

        if (DetectCycle(context.Definition))
        {
            logger.LogError("YAML sequence contains a cycle; unable to build playout");
            return BaseError.New("YAML sequence contains a cycle; unable to build playout");
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
        YamlPlayoutSchedule currentSchedule = activeSchedule;
        while (context.CurrentTime < finish)
        {
            // Check if we've crossed into a different schedule
            YamlPlayoutSchedule newSchedule = GetActiveSchedule(playoutDefinition, context.CurrentTime);
            if (!ReferenceEquals(currentSchedule, newSchedule))
            {
                string oldName = currentSchedule != null ? (string.IsNullOrWhiteSpace(currentSchedule.Name) ? "Unnamed" : currentSchedule.Name) : "Default";
                string newName = newSchedule != null ? (string.IsNullOrWhiteSpace(newSchedule.Name) ? "Unnamed" : newSchedule.Name) : "Default";
                logger.LogInformation(
                    "Schedule changed from '{OldSchedule}' to '{NewSchedule}' at {Time}",
                    oldName,
                    newName,
                    context.CurrentTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

                // Switch to the new schedule
                currentSchedule = newSchedule;

                // Get the new schedule's instructions
                List<YamlPlayoutInstruction> newPlayoutInstructions;
                if (newSchedule != null)
                {
                    newPlayoutInstructions = newSchedule.Playout;
                }
                else
                {
                    newPlayoutInstructions = playoutDefinition.Playout;
                }

                if (newPlayoutInstructions.Count == 0)
                {
                    logger.LogWarning("No playout instructions found for new schedule");
                    break;
                }

                // Create a new effective definition with the new schedule's instructions
                effectiveDefinition = new YamlPlayoutDefinition
                {
                    Import = playoutDefinition.Import,
                    Content = playoutDefinition.Content,
                    Sequence = playoutDefinition.Sequence,
                    Reset = newSchedule != null && newSchedule.Reset.Count > 0 ? newSchedule.Reset : playoutDefinition.Reset,
                    Playout = newPlayoutInstructions
                };

                // Flatten sequences for the new definition
                if (DetectCycle(effectiveDefinition))
                {
                    logger.LogError("YAML sequence contains a cycle in new schedule; unable to build playout");
                    break;
                }

                var newFlattenCount = 0;
                while (effectiveDefinition.Playout.Any(x => x is YamlPlayoutSequenceInstruction))
                {
                    if (newFlattenCount > 100)
                    {
                        logger.LogError(
                            "YAML playout definition contains sequence nesting that is too deep in new schedule");
                        break;
                    }

                    FlattenSequencesForDefinition(effectiveDefinition);
                    newFlattenCount++;
                }

                // Preserve existing state before creating new context
                var previousAddedItems = context.AddedItems.ToList();
                var previousAddedHistory = context.AddedHistory.ToList();
                var previousPreRollSequence = context.GetPreRollSequence();
                var previousPostRollSequence = context.GetPostRollSequence();
                var previousMidRollSequence = context.GetMidRollSequence();
                var previousWatermarkIds = context.GetChannelWatermarkIds();
                var previousGraphicsElements = context.GetGraphicsElements();

                context = new YamlPlayoutContext(playout, effectiveDefinition, 1)
                {
                    CurrentTime = context.CurrentTime,
                    InstructionIndex = 0  // Start from beginning of new schedule
                };

                // Restore all preserved state
                context.AddedItems.AddRange(previousAddedItems);
                context.AddedHistory.AddRange(previousAddedHistory);

                foreach (string preRoll in previousPreRollSequence)
                {
                    context.SetPreRollSequence(preRoll);
                }

                foreach (string postRoll in previousPostRollSequence)
                {
                    context.SetPostRollSequence(postRoll);
                }

                foreach (var midRoll in previousMidRollSequence)
                {
                    context.SetMidRollSequence(midRoll);
                }

                foreach (int watermarkId in previousWatermarkIds)
                {
                    context.SetChannelWatermarkId(watermarkId);
                }

                foreach (var (graphicsId, variablesJson) in previousGraphicsElements)
                {
                    context.SetGraphicsElement(graphicsId, variablesJson);
                }
            }

            if (context.InstructionIndex >= effectiveDefinition.Playout.Count)
            {
                logger.LogInformation("Reached the end of the YAML playout definition; stopping");
                break;
            }

            YamlPlayoutInstruction instruction = effectiveDefinition.Playout[context.InstructionIndex];
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

    private async Task<int> GetDaysToBuild(CancellationToken cancellationToken) =>
        await configElementRepository
            .GetValue<int>(ConfigElementKey.PlayoutDaysToBuild, cancellationToken)
            .IfNoneAsync(2);

    private static void FlattenSequences(YamlPlayoutContext context) =>
        FlattenSequencesForDefinition(context.Definition);

    private static void FlattenSequencesForDefinition(YamlPlayoutDefinition definition)
    {
        var rawInstructions = definition.Playout.ToImmutableList();
        definition.Playout.Clear();

        foreach (YamlPlayoutInstruction instruction in rawInstructions)
        {
            switch (instruction)
            {
                case YamlPlayoutSequenceInstruction sequenceInstruction:
                    IEnumerable<YamlPlayoutInstruction> sequenceInstructions = definition.Sequence
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

                            definition.Playout.Add(i);
                        }
                    }

                    break;
                default:
                    definition.Playout.Add(instruction);
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
            return Option<YamlPlayoutDefinition>.None;
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

    /// <summary>
    /// Finds the active schedule for the given date, or returns null if no schedule matches.
    /// </summary>
    private static YamlPlayoutSchedule GetActiveSchedule(YamlPlayoutDefinition definition, DateTimeOffset date)
    {
        if (definition.Schedules.Count == 0)
        {
            return null;
        }

        foreach (YamlPlayoutSchedule schedule in definition.Schedules)
        {
            if (IsDateInSchedule(schedule, date))
            {
                return schedule;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if a date falls within a scheduled playout's date range.
    /// </summary>
    private static bool IsDateInSchedule(YamlPlayoutSchedule schedule, DateTimeOffset date)
    {
        if (string.IsNullOrWhiteSpace(schedule.StartDate) || string.IsNullOrWhiteSpace(schedule.EndDate))
        {
            return false;
        }

        (int startMonth, int startDay, int? startYear) = ParseDate(schedule.StartDate);
        (int endMonth, int endDay, int? endYear) = ParseDate(schedule.EndDate);

        int currentYear = date.Year;
        int currentMonth = date.Month;
        int currentDay = date.Day;

        // Handle year-specific dates (YYYY-MM-DD format)
        if (startYear.HasValue && endYear.HasValue)
        {
            // Dates with years are always non-recurring (one-time events)
            var startDate = new DateTime(startYear.Value, startMonth, startDay);
            var endDate = new DateTime(endYear.Value, endMonth, endDay);
            var currentDate = new DateTime(currentYear, currentMonth, currentDay);

            return currentDate >= startDate && currentDate <= endDate;
        }

        if (startYear.HasValue || endYear.HasValue)
        {
            // Mixed format - not supported
            return false;
        }

        // Both dates are MM-DD format - always recurring
        var currentDayOfYear = new DateTime(currentYear, currentMonth, currentDay).DayOfYear;
        var startDayOfYear = new DateTime(currentYear, startMonth, startDay).DayOfYear;
        var endDayOfYear = new DateTime(currentYear, endMonth, endDay).DayOfYear;

        // Handle date ranges that span across the new year
        if (startDayOfYear <= endDayOfYear)
        {
            // Normal range (e.g., Jan 1 - March 31)
            return currentDayOfYear >= startDayOfYear && currentDayOfYear <= endDayOfYear;
        }

        // Range wraps around the year (e.g., Dec 1 - Jan 31)
        return currentDayOfYear >= startDayOfYear || currentDayOfYear <= endDayOfYear;
    }

    /// <summary>
    /// Parses a date string in MM-DD or YYYY-MM-DD format.
    /// Returns (month, day, year) where year is null for MM-DD format.
    /// </summary>
    private static (int month, int day, int? year) ParseDate(string dateStr)
    {
        string[] parts = dateStr.Split('-');

        if (parts.Length == 2)
        {
            // MM-DD format
            return (int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture), null);
        }

        if (parts.Length == 3)
        {
            // YYYY-MM-DD format
            return (int.Parse(parts[1], CultureInfo.InvariantCulture), int.Parse(parts[2], CultureInfo.InvariantCulture), int.Parse(parts[0], CultureInfo.InvariantCulture));
        }

        throw new FormatException($"Invalid date format: {dateStr}. Expected MM-DD or YYYY-MM-DD.");
    }
}
