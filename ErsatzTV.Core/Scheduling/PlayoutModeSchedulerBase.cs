﻿using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Scheduling;
using Humanizer;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling;

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public abstract class PlayoutModeSchedulerBase<T> : IPlayoutModeScheduler<T> where T : ProgramScheduleItem
{
    private readonly Random _random = new();

    protected PlayoutModeSchedulerBase(ILogger logger) => Logger = logger;
    protected ILogger Logger { get; }

    public abstract Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
        T scheduleItem,
        ProgramScheduleItem nextScheduleItem,
        DateTimeOffset hardStop,
        CancellationToken cancellationToken);

    public static DateTimeOffset GetFillerStartTimeAfter(
        PlayoutBuilderState state,
        ProgramScheduleItem scheduleItem,
        DateTimeOffset hardStop)
    {
        DateTimeOffset startTime = GetStartTimeAfter(state, scheduleItem, Option<ILogger>.None);

        // filler should always stop at the hard stop
        if (hardStop < startTime)
        {
            startTime = hardStop;
        }

        return startTime;
    }

    public static DateTimeOffset GetStartTimeAfter(
        PlayoutBuilderState state,
        ProgramScheduleItem scheduleItem,
        Option<ILogger> maybeLogger,
        bool isPeek = false)
    {
        DateTimeOffset startTime = state.CurrentTime.ToLocalTime();

        bool isIncomplete = !isPeek &&
                            (scheduleItem is ProgramScheduleItemMultiple && state.MultipleRemaining.IsSome ||
                             scheduleItem is ProgramScheduleItemDuration && state.DurationFinish.IsSome ||
                             scheduleItem is ProgramScheduleItemFlood && state.InFlood ||
                             scheduleItem is ProgramScheduleItemDuration && state.InDurationFiller);

        if (scheduleItem.StartType == StartType.Fixed && !isIncomplete)
        {
            TimeSpan itemStartTime = scheduleItem.StartTime.GetValueOrDefault();

            DateTime date = startTime.Date;
            DateTimeOffset result = new DateTimeOffset(
                    date.Year,
                    date.Month,
                    date.Day,
                    0,
                    0,
                    0,
                    TimeZoneInfo.Local.GetUtcOffset(
                        new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Local)))
                .Add(itemStartTime);

            // Serilog.Log.Logger.Debug(
            //     "StartTimeOfDay: {StartTimeOfDay} Item Start Time: {ItemStartTime}",
            //     startTime.TimeOfDay.TotalMilliseconds,
            //     itemStartTime.TotalMilliseconds);

            // need to wrap to the next day if appropriate
            FixedStartTimeBehavior? fixedStartTimeBehavior = scheduleItem.FixedStartTimeBehavior;
            if (fixedStartTimeBehavior is null && scheduleItem.ProgramSchedule is not null)
            {
                fixedStartTimeBehavior = scheduleItem.ProgramSchedule.FixedStartTimeBehavior;
            }

            switch (fixedStartTimeBehavior)
            {
                case FixedStartTimeBehavior.Flexible:
                    // if we are peeking from a flood and the flexible time is in the past,
                    // we should use the next day's time to allow the flood to continue.
                    if (isPeek && state.InFlood && startTime > result)
                    {
                        startTime = result.AddDays(1);
                    }
                    // otherwise, only wait for times on the same day
                    else if (result.Day == startTime.Day && result.TimeOfDay > startTime.TimeOfDay)
                    {
                        startTime = result;
                    }

                    break;
                case FixedStartTimeBehavior.Strict:
                default:
                    startTime = startTime.TimeOfDay > itemStartTime ? result.AddDays(1) : result;
                    break;
            }

            TimeSpan gap = startTime - state.CurrentTime;
            if (gap > TimeSpan.FromHours(1) && fixedStartTimeBehavior == FixedStartTimeBehavior.Strict &&
                result.TimeOfDay < state.CurrentTime.ToLocalTime().TimeOfDay)
            {
                foreach (ILogger logger in maybeLogger)
                {
                    logger.LogWarning(
                        "Offline playout gap of {Gap} caused by strict fixed start time {StartTime} before current time {CurrentTime} on schedule {Name}",
                        gap.Humanize(),
                        result.TimeOfDay,
                        state.CurrentTime.TimeOfDay,
                        scheduleItem.ProgramSchedule?.Name ?? "unknown");
                }
            }
        }

        return startTime;
    }

    protected Tuple<PlayoutBuilderState, List<PlayoutItem>> AddTailFiller(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
        ProgramScheduleItem scheduleItem,
        List<PlayoutItem> playoutItems,
        DateTimeOffset nextItemStart,
        CancellationToken cancellationToken)
    {
        var newItems = new List<PlayoutItem>(playoutItems);
        PlayoutBuilderState nextState = playoutBuilderState;

        if (scheduleItem.TailFiller != null)
        {
            IMediaCollectionEnumerator enumerator =
                collectionEnumerators[CollectionKey.ForFillerPreset(scheduleItem.TailFiller)];

            while (enumerator.Current.IsSome && nextState.CurrentTime < nextItemStart)
            {
                MediaItem mediaItem = enumerator.Current.ValueUnsafe();

                TimeSpan itemDuration = DurationForMediaItem(mediaItem);
                TimeSpan inPoint = InPointForMediaItem(mediaItem);

                if (nextState.CurrentTime + itemDuration > nextItemStart)
                {
                    Logger.LogDebug(
                        "Filler with duration {Duration:hh\\:mm\\:ss} will go past next item start {NextItemStart}",
                        itemDuration,
                        nextItemStart);

                    break;
                }

                var playoutItem = new PlayoutItem
                {
                    PlayoutId = playoutBuilderState.PlayoutId,
                    MediaItemId = IdForMediaItem(mediaItem),
                    Start = nextState.CurrentTime.UtcDateTime,
                    Finish = nextState.CurrentTime.UtcDateTime + itemDuration,
                    InPoint = inPoint,
                    OutPoint = inPoint + itemDuration,
                    FillerKind = FillerKind.Tail,
                    GuideGroup = nextState.NextGuideGroup,
                    DisableWatermarks = !scheduleItem.TailFiller.AllowWatermarks,
                    ChapterTitle = ChapterTitleForMediaItem(mediaItem)
                };

                newItems.Add(playoutItem);

                nextState = nextState with
                {
                    CurrentTime = nextState.CurrentTime + itemDuration
                };

                enumerator.MoveNext();
            }
        }

        return Tuple(nextState, newItems);
    }

    protected Tuple<PlayoutBuilderState, List<PlayoutItem>> AddFallbackFiller(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
        ProgramScheduleItem scheduleItem,
        List<PlayoutItem> playoutItems,
        DateTimeOffset nextItemStart,
        CancellationToken cancellationToken)
    {
        var newItems = new List<PlayoutItem>(playoutItems);
        PlayoutBuilderState nextState = playoutBuilderState;

        if (scheduleItem.FallbackFiller != null && playoutBuilderState.CurrentTime < nextItemStart)
        {
            IMediaCollectionEnumerator enumerator =
                collectionEnumerators[CollectionKey.ForFillerPreset(scheduleItem.FallbackFiller)];

            foreach (MediaItem mediaItem in enumerator.Current)
            {
                var playoutItem = new PlayoutItem
                {
                    PlayoutId = playoutBuilderState.PlayoutId,
                    MediaItemId = mediaItem.Id,
                    Start = nextState.CurrentTime.UtcDateTime,
                    Finish = nextItemStart.UtcDateTime,
                    InPoint = TimeSpan.Zero,
                    OutPoint = TimeSpan.Zero,
                    GuideGroup = nextState.NextGuideGroup,
                    FillerKind = FillerKind.Fallback,
                    DisableWatermarks = !scheduleItem.FallbackFiller.AllowWatermarks
                };

                newItems.Add(playoutItem);

                nextState = nextState with
                {
                    CurrentTime = nextItemStart.UtcDateTime
                };

                enumerator.MoveNext();
            }
        }

        return Tuple(nextState, newItems);
    }

    protected static TimeSpan DurationForMediaItem(MediaItem mediaItem)
    {
        if (mediaItem is Image image)
        {
            return TimeSpan.FromSeconds(image.ImageMetadata.Head().DurationSeconds ?? Image.DefaultSeconds);
        }

        MediaVersion version = mediaItem.GetHeadVersion();

        if (mediaItem is RemoteStream remoteStream)
        {
            return version.Duration == TimeSpan.Zero && remoteStream.Duration.HasValue
                ? remoteStream.Duration.Value
                : version.Duration;
        }

        return version.Duration;
    }

    private static TimeSpan InPointForMediaItem(MediaItem mediaItem) =>
        mediaItem switch
        {
            ChapterMediaItem c => c.MediaVersion.InPoint,
            _ => TimeSpan.Zero
        };

    private static int IdForMediaItem(MediaItem mediaItem) =>
        mediaItem switch
        {
            ChapterMediaItem c => c.MediaItemId,
            _ => mediaItem.Id
        };

    private static string ChapterTitleForMediaItem(MediaItem mediaItem) =>
        mediaItem switch
        {
            ChapterMediaItem c => c.MediaVersion.Title,
            _ => null
        };

    protected static List<MediaChapter> ChaptersForMediaItem(MediaItem mediaItem)
    {
        MediaVersion version = mediaItem.GetHeadVersion();
        return Optional(version.Chapters).Flatten().OrderBy(c => c.StartTime).ToList();
    }

    protected void LogScheduledItem(
        ProgramScheduleItem scheduleItem,
        MediaItem mediaItem,
        DateTimeOffset startTime) =>
        Logger.LogDebug(
            "Scheduling media item: {ScheduleItemNumber} / {CollectionType} / {MediaItemId} - {MediaItemTitle} / {StartTime}",
            scheduleItem.Index,
            scheduleItem.CollectionType,
            mediaItem.Id,
            PlayoutBuilder.DisplayTitle(mediaItem),
            startTime);

    internal List<PlayoutItem> AddFiller(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> enumerators,
        ProgramScheduleItem scheduleItem,
        PlayoutItem playoutItem,
        List<MediaChapter> chapters,
        bool log,
        CancellationToken cancellationToken)
    {
        var result = new List<PlayoutItem>();

        var allFiller = Optional(scheduleItem.PreRollFiller)
            .Append(Optional(scheduleItem.MidRollFiller))
            .Append(Optional(scheduleItem.PostRollFiller))
            .ToList();

        // multiple pad-to-nearest-minute values are invalid; use no filler
        if (allFiller.Count(f => f.FillerMode == FillerMode.Pad && f.PadToNearestMinute.HasValue) > 1)
        {
            Logger.LogError("Multiple pad-to-nearest-minute values are invalid; no filler will be used");
            return [playoutItem];
        }

        // missing pad-to-nearest-minute value is invalid; use no filler
        FillerPreset invalidPadFiller = allFiller
            .FirstOrDefault(f => f.FillerMode == FillerMode.Pad && !f.PadToNearestMinute.HasValue);
        if (invalidPadFiller is not null)
        {
            Logger.LogError(
                "Pad filler ({Filler}) without pad-to-nearest-minute value is invalid; no filler will be used",
                invalidPadFiller.Name);
            return [playoutItem];
        }

        List<MediaChapter> effectiveChapters = chapters;
        if (allFiller.All(fp => fp.FillerKind != FillerKind.MidRoll) || effectiveChapters.Count <= 1)
        {
            effectiveChapters = [];
        }

        // convert mid-roll to post-roll if we have no chapters
        if (allFiller.Any(f => f.FillerKind is FillerKind.MidRoll) && effectiveChapters.Count == 0)
        {
            Logger.LogInformation(
                "Converting mid-roll filler preset to post-roll for content that has no chapter markers");

            var toRemove = allFiller.Filter(f => f.FillerKind is FillerKind.MidRoll).ToList();
            allFiller.RemoveAll(toRemove.Contains);

            foreach (FillerPreset midRollFiller in toRemove)
            {
                var clone = new FillerPreset
                {
                    FillerKind = FillerKind.PostRoll,
                    FillerMode = midRollFiller.FillerMode,
                    Duration = midRollFiller.Duration,
                    Count = midRollFiller.Count,
                    PadToNearestMinute = midRollFiller.PadToNearestMinute,
                    AllowWatermarks = midRollFiller.AllowWatermarks,
                    CollectionType = midRollFiller.CollectionType,
                    CollectionId = midRollFiller.CollectionId,
                    Collection = midRollFiller.Collection,
                    MediaItemId = midRollFiller.MediaItemId,
                    MediaItem = midRollFiller.MediaItem,
                    MultiCollectionId = midRollFiller.MultiCollectionId,
                    MultiCollection = midRollFiller.MultiCollection,
                    SmartCollectionId = midRollFiller.SmartCollectionId,
                    SmartCollection = midRollFiller.SmartCollection,
                    PlaylistId = midRollFiller.PlaylistId,
                    Playlist = midRollFiller.Playlist
                };

                allFiller.Add(clone);
            }
        }

        // convert playlist filler
        if (allFiller.Any(f => f.CollectionType is CollectionType.Playlist))
        {
            var toRemove = allFiller.Filter(f => f.CollectionType is CollectionType.Playlist)
                .ToList();
            allFiller.RemoveAll(toRemove.Contains);

            foreach (FillerPreset playlistFiller in toRemove)
            {
                var clone = new FillerPreset
                {
                    FillerKind = playlistFiller.FillerKind,
                    FillerMode = playlistFiller.FillerMode,
                    Duration = playlistFiller.Duration,
                    Count = playlistFiller.Count,
                    PadToNearestMinute = playlistFiller.PadToNearestMinute,
                    AllowWatermarks = playlistFiller.AllowWatermarks,
                    CollectionType = playlistFiller.CollectionType,
                    CollectionId = playlistFiller.CollectionId,
                    Collection = playlistFiller.Collection,
                    MediaItemId = playlistFiller.MediaItemId,
                    MediaItem = playlistFiller.MediaItem,
                    MultiCollectionId = playlistFiller.MultiCollectionId,
                    MultiCollection = playlistFiller.MultiCollection,
                    SmartCollectionId = playlistFiller.SmartCollectionId,
                    SmartCollection = playlistFiller.SmartCollection,
                    PlaylistId = playlistFiller.PlaylistId,
                    Playlist = playlistFiller.Playlist
                };

                // if filler count is 2, we need to schedule 2 * (number of items in one full playlist iteration)
                IMediaCollectionEnumerator fillerEnumerator =
                    enumerators[CollectionKey.ForFillerPreset(playlistFiller)];
                if (fillerEnumerator is PlaylistEnumerator playlistEnumerator)
                {
                    clone.Count *= playlistEnumerator.CountForFiller;
                }

                allFiller.Add(clone);
            }
        }

        foreach (FillerPreset filler in allFiller.Filter(f =>
                     f.FillerKind == FillerKind.PreRoll && f.FillerMode != FillerMode.Pad))
        {
            switch (filler.FillerMode)
            {
                case FillerMode.Duration when filler.Duration.HasValue:
                    IMediaCollectionEnumerator e1 = enumerators[CollectionKey.ForFillerPreset(filler)];
                    result.AddRange(
                        AddDurationFiller(
                            playoutBuilderState,
                            e1,
                            filler.Duration.Value,
                            scheduleItem.GuideMode == GuideMode.Filler ? FillerKind.GuideMode : FillerKind.PreRoll,
                            filler.AllowWatermarks,
                            log,
                            cancellationToken));
                    break;
                case FillerMode.Count when filler.Count.HasValue:
                    IMediaCollectionEnumerator e2 = enumerators[CollectionKey.ForFillerPreset(filler)];
                    result.AddRange(
                        AddCountFiller(
                            playoutBuilderState,
                            e2,
                            filler.Count.Value,
                            scheduleItem.GuideMode == GuideMode.Filler ? FillerKind.GuideMode : FillerKind.PreRoll,
                            filler.AllowWatermarks,
                            cancellationToken));
                    break;
                case FillerMode.RandomCount when filler.Count.HasValue:
                    IMediaCollectionEnumerator e3 = enumerators[CollectionKey.ForFillerPreset(filler)];
                    result.AddRange(
                        AddRandomCountFiller(
                            playoutBuilderState,
                            e3,
                            filler.Count.Value,
                            scheduleItem.GuideMode == GuideMode.Filler ? FillerKind.GuideMode : FillerKind.PreRoll,
                            filler.AllowWatermarks,
                            cancellationToken));
                    break;
            }
        }

        if (effectiveChapters.Count <= 1)
        {
            result.Add(playoutItem);
        }
        else
        {
            foreach (FillerPreset filler in allFiller.Filter(f =>
                         f.FillerKind == FillerKind.MidRoll && f.FillerMode != FillerMode.Pad))
            {
                List<MediaChapter> filteredChapters = FillerExpression.FilterChapters(
                    filler.Expression,
                    effectiveChapters,
                    playoutItem);
                if (filteredChapters.Count <= 1)
                {
                    result.Add(playoutItem);
                    continue;
                }

                switch (filler.FillerMode)
                {
                    case FillerMode.Duration when filler.Duration.HasValue:
                        IMediaCollectionEnumerator e1 = enumerators[CollectionKey.ForFillerPreset(filler)];
                        for (var i = 0; i < filteredChapters.Count; i++)
                        {
                            result.Add(playoutItem.ForChapter(filteredChapters[i]));
                            if (i < filteredChapters.Count - 1)
                            {
                                result.AddRange(
                                    AddDurationFiller(
                                        playoutBuilderState,
                                        e1,
                                        filler.Duration.Value,
                                        scheduleItem.GuideMode == GuideMode.Filler
                                            ? FillerKind.GuideMode
                                            : FillerKind.MidRoll,
                                        filler.AllowWatermarks,
                                        log,
                                        cancellationToken));
                            }
                        }

                        break;
                    case FillerMode.Count when filler.Count.HasValue:
                        IMediaCollectionEnumerator e2 = enumerators[CollectionKey.ForFillerPreset(filler)];
                        for (var i = 0; i < filteredChapters.Count; i++)
                        {
                            result.Add(playoutItem.ForChapter(filteredChapters[i]));
                            if (i < filteredChapters.Count - 1)
                            {
                                result.AddRange(
                                    AddCountFiller(
                                        playoutBuilderState,
                                        e2,
                                        filler.Count.Value,
                                        scheduleItem.GuideMode == GuideMode.Filler
                                            ? FillerKind.GuideMode
                                            : FillerKind.MidRoll,
                                        filler.AllowWatermarks,
                                        cancellationToken));
                            }
                        }

                        break;
                    case FillerMode.RandomCount when filler.Count.HasValue:
                        IMediaCollectionEnumerator e3 = enumerators[CollectionKey.ForFillerPreset(filler)];
                        for (var i = 0; i < filteredChapters.Count; i++)
                        {
                            result.Add(playoutItem.ForChapter(filteredChapters[i]));
                            if (i < filteredChapters.Count - 1)
                            {
                                result.AddRange(
                                    AddRandomCountFiller(
                                        playoutBuilderState,
                                        e3,
                                        filler.Count.Value,
                                        scheduleItem.GuideMode == GuideMode.Filler
                                            ? FillerKind.GuideMode
                                            : FillerKind.MidRoll,
                                        filler.AllowWatermarks,
                                        cancellationToken));
                            }
                        }

                        break;
                }
            }
        }

        foreach (FillerPreset filler in allFiller.Filter(f =>
                     f.FillerKind == FillerKind.PostRoll && f.FillerMode != FillerMode.Pad))
        {
            switch (filler.FillerMode)
            {
                case FillerMode.Duration when filler.Duration.HasValue:
                    IMediaCollectionEnumerator e1 = enumerators[CollectionKey.ForFillerPreset(filler)];
                    result.AddRange(
                        AddDurationFiller(
                            playoutBuilderState,
                            e1,
                            filler.Duration.Value,
                            scheduleItem.GuideMode == GuideMode.Filler ? FillerKind.GuideMode : FillerKind.PostRoll,
                            filler.AllowWatermarks,
                            log,
                            cancellationToken));
                    break;
                case FillerMode.Count when filler.Count.HasValue:
                    IMediaCollectionEnumerator e2 = enumerators[CollectionKey.ForFillerPreset(filler)];
                    result.AddRange(
                        AddCountFiller(
                            playoutBuilderState,
                            e2,
                            filler.Count.Value,
                            scheduleItem.GuideMode == GuideMode.Filler ? FillerKind.GuideMode : FillerKind.PostRoll,
                            filler.AllowWatermarks,
                            cancellationToken));
                    break;
                case FillerMode.RandomCount when filler.Count.HasValue:
                    IMediaCollectionEnumerator e3 = enumerators[CollectionKey.ForFillerPreset(filler)];
                    result.AddRange(
                        AddRandomCountFiller(
                            playoutBuilderState,
                            e3,
                            filler.Count.Value,
                            scheduleItem.GuideMode == GuideMode.Filler ? FillerKind.GuideMode : FillerKind.PostRoll,
                            filler.AllowWatermarks,
                            cancellationToken));
                    break;
            }
        }

        // after all non-padded filler has been added, figure out padding
        foreach (FillerPreset padFiller in Optional(
                     allFiller.FirstOrDefault(f => f.FillerMode == FillerMode.Pad && f.PadToNearestMinute.HasValue)))
        {
            var totalDuration = TimeSpan.FromTicks(result.Sum(pi => (pi.Finish - pi.Start).Ticks));

            List<MediaChapter> filteredChapters =
                FillerExpression.FilterChapters(padFiller.Expression, effectiveChapters, playoutItem);

            FillerKind fillerKind = padFiller.FillerKind;
            if (filteredChapters.Count <= 1 && effectiveChapters.Count > 1)
            {
                fillerKind = FillerKind.PostRoll;
            }

            // add primary content to totalDuration only if it hasn't already been added
            if (result.All(pi => pi.MediaItemId != playoutItem.MediaItemId))
            {
                totalDuration += TimeSpan.FromTicks(filteredChapters.Sum(c => (c.EndTime - c.StartTime).Ticks));
            }

            int currentMinute = (playoutItem.StartOffset + totalDuration).Minute;
            // ReSharper disable once PossibleInvalidOperationException
            int targetMinute = (currentMinute + padFiller.PadToNearestMinute.Value - 1) /
                padFiller.PadToNearestMinute.Value * padFiller.PadToNearestMinute.Value;

            DateTimeOffset almostTargetTime = playoutItem.StartOffset + totalDuration -
                                              TimeSpan.FromMinutes(currentMinute) +
                                              TimeSpan.FromMinutes(targetMinute);

            var targetTime = new DateTimeOffset(
                almostTargetTime.Year,
                almostTargetTime.Month,
                almostTargetTime.Day,
                almostTargetTime.Hour,
                almostTargetTime.Minute,
                0,
                almostTargetTime.Offset);

            // ensure filler works for content less than one minute
            if (targetTime <= playoutItem.StartOffset + totalDuration)
            {
                targetTime = targetTime.AddMinutes(padFiller.PadToNearestMinute.Value);
            }

            TimeSpan remainingToFill = targetTime - totalDuration - playoutItem.StartOffset;

            // Logger.LogInformation(
            //     "Total duration {TotalDuration}; need to fill {TimeSpan} to pad properly from {StartTime} to {TargetTime}",
            //     totalDuration,
            //     remainingToFill,
            //     playoutItem.StartOffset,
            //     targetTime);

            switch (fillerKind)
            {
                case FillerKind.PreRoll:
                    IMediaCollectionEnumerator pre1 = enumerators[CollectionKey.ForFillerPreset(padFiller)];
                    result.InsertRange(
                        0,
                        AddDurationFiller(
                            playoutBuilderState,
                            pre1,
                            remainingToFill,
                            scheduleItem.GuideMode == GuideMode.Filler ? FillerKind.GuideMode : FillerKind.PreRoll,
                            padFiller.AllowWatermarks,
                            log,
                            cancellationToken));
                    totalDuration = TimeSpan.FromTicks(result.Sum(pi => (pi.Finish - pi.Start).Ticks));
                    remainingToFill = targetTime - totalDuration - playoutItem.StartOffset;
                    if (remainingToFill > TimeSpan.Zero)
                    {
                        result.InsertRange(
                            0,
                            FallbackFillerForPad(
                                playoutBuilderState,
                                enumerators,
                                scheduleItem,
                                remainingToFill,
                                cancellationToken));
                    }

                    break;
                case FillerKind.MidRoll:
                    IMediaCollectionEnumerator mid1 = enumerators[CollectionKey.ForFillerPreset(padFiller)];
                    var fillerQueue = new Queue<PlayoutItem>(
                        AddDurationFiller(
                            playoutBuilderState,
                            mid1,
                            remainingToFill,
                            scheduleItem.GuideMode == GuideMode.Filler ? FillerKind.GuideMode : FillerKind.MidRoll,
                            padFiller.AllowWatermarks,
                            log,
                            cancellationToken));
                    TimeSpan average = filteredChapters.Count <= 1
                        ? remainingToFill
                        : remainingToFill / (filteredChapters.Count - 1);
                    TimeSpan filled = TimeSpan.Zero;

                    // remove post-roll to add after mid-roll/content
                    var postRoll = result.Where(i => i.FillerKind == FillerKind.PostRoll).ToList();
                    result.RemoveAll(i => i.FillerKind == FillerKind.PostRoll);

                    for (var i = 0; i < filteredChapters.Count; i++)
                    {
                        result.Add(playoutItem.ForChapter(filteredChapters[i]));
                        if (i < filteredChapters.Count - 1)
                        {
                            TimeSpan current = TimeSpan.Zero;
                            while (current < average && filled < remainingToFill)
                            {
                                if (fillerQueue.TryDequeue(out PlayoutItem fillerItem))
                                {
                                    result.Add(fillerItem);
                                    current += fillerItem.Finish - fillerItem.Start;
                                    filled += fillerItem.Finish - fillerItem.Start;
                                }
                                else
                                {
                                    TimeSpan leftInThisBreak = average - current;
                                    TimeSpan leftOverall = remainingToFill - filled;

                                    TimeSpan maxThisBreak = leftOverall < leftInThisBreak
                                        ? leftOverall
                                        : leftInThisBreak;

                                    Option<PlayoutItem> maybeFallback = FallbackFillerForPad(
                                        playoutBuilderState,
                                        enumerators,
                                        scheduleItem,
                                        i < filteredChapters.Count - 1 ? maxThisBreak : leftOverall,
                                        cancellationToken);

                                    foreach (PlayoutItem fallback in maybeFallback)
                                    {
                                        current += fallback.Finish - fallback.Start;
                                        filled += fallback.Finish - fallback.Start;
                                        result.Add(fallback);
                                    }
                                }
                            }
                        }
                    }

                    result.AddRange(postRoll);

                    break;
                case FillerKind.PostRoll:
                    IMediaCollectionEnumerator post1 = enumerators[CollectionKey.ForFillerPreset(padFiller)];
                    result.AddRange(
                        AddDurationFiller(
                            playoutBuilderState,
                            post1,
                            remainingToFill,
                            scheduleItem.GuideMode == GuideMode.Filler ? FillerKind.GuideMode : FillerKind.PostRoll,
                            padFiller.AllowWatermarks,
                            log,
                            cancellationToken));
                    totalDuration = TimeSpan.FromTicks(result.Sum(pi => (pi.Finish - pi.Start).Ticks));
                    remainingToFill = targetTime - totalDuration - playoutItem.StartOffset;
                    if (remainingToFill > TimeSpan.Zero)
                    {
                        result.AddRange(
                            FallbackFillerForPad(
                                playoutBuilderState,
                                enumerators,
                                scheduleItem,
                                remainingToFill,
                                cancellationToken));
                    }

                    break;
            }
        }

        // fix times on each playout item
        DateTimeOffset currentTime = playoutItem.StartOffset;
        for (var i = 0; i < result.Count; i++)
        {
            PlayoutItem item = result[i];
            TimeSpan duration = item.Finish - item.Start;
            item.Start = currentTime.UtcDateTime;
            item.Finish = (currentTime + duration).UtcDateTime;
            currentTime = item.FinishOffset;
        }

        return result;
    }

    private static List<PlayoutItem> AddCountFiller(
        PlayoutBuilderState playoutBuilderState,
        IMediaCollectionEnumerator enumerator,
        int count,
        FillerKind fillerKind,
        bool allowWatermarks,
        CancellationToken cancellationToken)
    {
        var result = new List<PlayoutItem>();

        for (var i = 0; i < count; i++)
        {
            foreach (MediaItem mediaItem in enumerator.Current)
            {
                TimeSpan itemDuration = DurationForMediaItem(mediaItem);
                TimeSpan inPoint = InPointForMediaItem(mediaItem);

                var playoutItem = new PlayoutItem
                {
                    PlayoutId = playoutBuilderState.PlayoutId,
                    MediaItemId = IdForMediaItem(mediaItem),
                    Start = new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Finish = new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc) + itemDuration,
                    InPoint = inPoint,
                    OutPoint = inPoint + itemDuration,
                    GuideGroup = playoutBuilderState.NextGuideGroup,
                    FillerKind = fillerKind,
                    DisableWatermarks = !allowWatermarks,
                    ChapterTitle = ChapterTitleForMediaItem(mediaItem)
                };

                result.Add(playoutItem);
                enumerator.MoveNext();
            }
        }

        return result;
    }

    private List<PlayoutItem> AddDurationFiller(
        PlayoutBuilderState playoutBuilderState,
        IMediaCollectionEnumerator enumerator,
        TimeSpan duration,
        FillerKind fillerKind,
        bool allowWatermarks,
        bool log,
        CancellationToken cancellationToken)
    {
        var result = new List<PlayoutItem>();

        TimeSpan remainingToFill = duration;
        while (enumerator.Current.IsSome && remainingToFill > TimeSpan.Zero &&
               remainingToFill >= enumerator.MinimumDuration)
        {
            foreach (MediaItem mediaItem in enumerator.Current)
            {
                TimeSpan itemDuration = DurationForMediaItem(mediaItem);
                TimeSpan inPoint = InPointForMediaItem(mediaItem);

                if (remainingToFill - itemDuration >= TimeSpan.Zero)
                {
                    var playoutItem = new PlayoutItem
                    {
                        PlayoutId = playoutBuilderState.PlayoutId,
                        MediaItemId = IdForMediaItem(mediaItem),
                        Start = new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                        Finish = new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc) + itemDuration,
                        InPoint = inPoint,
                        OutPoint = inPoint + itemDuration,
                        GuideGroup = playoutBuilderState.NextGuideGroup,
                        FillerKind = fillerKind,
                        DisableWatermarks = !allowWatermarks,
                        ChapterTitle = ChapterTitleForMediaItem(mediaItem)
                    };

                    remainingToFill -= itemDuration;
                    result.Add(playoutItem);
                    enumerator.MoveNext();
                }
                else
                {
                    if (log)
                    {
                        Logger.LogDebug(
                            "Filler item is too long {FillerDuration:g} to fill {GapDuration:g}; skipping to next filler item",
                            itemDuration,
                            remainingToFill);
                    }

                    enumerator.MoveNext();
                }
            }
        }

        return result;
    }

    private static Option<PlayoutItem> FallbackFillerForPad(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> enumerators,
        ProgramScheduleItem scheduleItem,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        if (scheduleItem.FallbackFiller != null)
        {
            IMediaCollectionEnumerator enumerator =
                enumerators[CollectionKey.ForFillerPreset(scheduleItem.FallbackFiller)];

            foreach (MediaItem mediaItem in enumerator.Current)
            {
                var result = new PlayoutItem
                {
                    PlayoutId = playoutBuilderState.PlayoutId,
                    MediaItemId = mediaItem.Id,
                    Start = new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Finish = new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc) + duration,
                    InPoint = TimeSpan.Zero,
                    OutPoint = TimeSpan.Zero,
                    GuideGroup = playoutBuilderState.NextGuideGroup,
                    FillerKind = FillerKind.Fallback,
                    DisableWatermarks = !scheduleItem.FallbackFiller.AllowWatermarks
                };

                enumerator.MoveNext();

                return result;
            }
        }

        return None;
    }

    private List<PlayoutItem> AddRandomCountFiller(
        PlayoutBuilderState playoutBuilderState,
        IMediaCollectionEnumerator enumerator,
        int count,
        FillerKind fillerKind,
        bool allowWatermarks,
        CancellationToken cancellationToken)
    {
        var result = new List<PlayoutItem>();
        // randomCount is from 0 to count.
        int randomCount = _random.Next(count + 1);

        if (randomCount != 0)
        {
            result = AddCountFiller(
                playoutBuilderState,
                enumerator,
                randomCount,
                fillerKind,
                allowWatermarks,
                cancellationToken);
        }

        return result;
    }
}
