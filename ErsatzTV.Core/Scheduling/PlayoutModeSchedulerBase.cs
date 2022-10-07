using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling;

public abstract class PlayoutModeSchedulerBase<T> : IPlayoutModeScheduler<T> where T : ProgramScheduleItem
{
    protected readonly ILogger _logger;

    protected PlayoutModeSchedulerBase(ILogger logger) => _logger = logger;

    public abstract Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
        T scheduleItem,
        ProgramScheduleItem nextScheduleItem,
        DateTimeOffset hardStop);

    public static DateTimeOffset GetFillerStartTimeAfter(
        PlayoutBuilderState state,
        ProgramScheduleItem scheduleItem,
        DateTimeOffset hardStop
    )
    {
        DateTimeOffset startTime = GetStartTimeAfter(state, scheduleItem);
        
        // filler should always stop at the hard stop
        if (hardStop < startTime)
        {
            startTime = hardStop;
        }

        return startTime;
    }

    public static DateTimeOffset GetStartTimeAfter(PlayoutBuilderState state, ProgramScheduleItem scheduleItem)
    {
        DateTimeOffset startTime = state.CurrentTime.ToLocalTime();

        bool isIncomplete = scheduleItem is ProgramScheduleItemMultiple && state.MultipleRemaining.IsSome ||
                            scheduleItem is ProgramScheduleItemDuration && state.DurationFinish.IsSome ||
                            scheduleItem is ProgramScheduleItemFlood && state.InFlood ||
                            scheduleItem is ProgramScheduleItemDuration && state.InDurationFiller;

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

            // DateTimeOffset result = startTime.Date + itemStartTime;
            // need to wrap to the next day if appropriate
            startTime = startTime.TimeOfDay > itemStartTime ? result.AddDays(1) : result;
        }

        return startTime;
    }

    protected Tuple<PlayoutBuilderState, List<PlayoutItem>> AddTailFiller(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
        ProgramScheduleItem scheduleItem,
        List<PlayoutItem> playoutItems,
        DateTimeOffset nextItemStart)
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

                if (nextState.CurrentTime + itemDuration > nextItemStart)
                {
                    _logger.LogDebug(
                        "Filler with duration {Duration} will go past next item start {NextItemStart}",
                        itemDuration,
                        nextItemStart);

                    break;
                }

                var playoutItem = new PlayoutItem
                {
                    MediaItemId = mediaItem.Id,
                    Start = nextState.CurrentTime.UtcDateTime,
                    Finish = nextState.CurrentTime.UtcDateTime + itemDuration,
                    InPoint = TimeSpan.Zero,
                    OutPoint = itemDuration,
                    FillerKind = FillerKind.Tail,
                    GuideGroup = nextState.NextGuideGroup,
                    DisableWatermarks = !scheduleItem.TailFiller.AllowWatermarks
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
        DateTimeOffset nextItemStart)
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
        MediaVersion version = mediaItem.GetHeadVersion();
        return version.Duration;
    }

    protected static List<MediaChapter> ChaptersForMediaItem(MediaItem mediaItem)
    {
        MediaVersion version = mediaItem.GetHeadVersion();
        return version.Chapters;
    }

    protected void LogScheduledItem(
        ProgramScheduleItem scheduleItem,
        MediaItem mediaItem,
        DateTimeOffset startTime) =>
        _logger.LogDebug(
            "Scheduling media item: {ScheduleItemNumber} / {CollectionType} / {MediaItemId} - {MediaItemTitle} / {StartTime}",
            scheduleItem.Index,
            scheduleItem.CollectionType,
            mediaItem.Id,
            PlayoutBuilder.DisplayTitle(mediaItem),
            startTime);

    internal static DateTimeOffset CalculateEndTimeWithFiller(
        Dictionary<CollectionKey, IMediaCollectionEnumerator> enumerators,
        ProgramScheduleItem scheduleItem,
        DateTimeOffset itemStartTime,
        TimeSpan itemDuration,
        List<MediaChapter> chapters)
    {
        var allFiller = Optional(scheduleItem.PreRollFiller)
            .Append(Optional(scheduleItem.MidRollFiller))
            .Append(Optional(scheduleItem.PostRollFiller))
            .ToList();

        // multiple pad-to-nearest-minute values are invalid; use no filler
        if (allFiller.Count(f => f.FillerMode == FillerMode.Pad && f.PadToNearestMinute.HasValue) > 1)
        {
            return itemStartTime + itemDuration;
        }

        TimeSpan totalDuration = itemDuration;
        foreach (FillerPreset filler in allFiller)
        {
            switch (filler.FillerKind, filler.FillerMode)
            {
                case (FillerKind.MidRoll, FillerMode.Duration) when filler.Duration.HasValue:
                    IMediaCollectionEnumerator mrde = enumerators[CollectionKey.ForFillerPreset(filler)];
                    var mrdePeekOffset = 0;
                    for (var i = 0; i < chapters.Count - 1; i++)
                    {
                        TimeSpan midRollDuration = filler.Duration.Value;
                        while (mrde.Peek(mrdePeekOffset))
                        {
                            foreach (MediaItem mediaItem in mrde.Peek(mrdePeekOffset))
                            {
                                TimeSpan currentDuration = DurationForMediaItem(mediaItem);
                                midRollDuration -= currentDuration;
                                if (midRollDuration >= TimeSpan.Zero)
                                {
                                    totalDuration += currentDuration;
                                    mrdePeekOffset++;
                                }
                            }

                            if (midRollDuration < TimeSpan.Zero)
                            {
                                break;
                            }
                        }
                    }

                    break;
                case (FillerKind.MidRoll, FillerMode.Count) when filler.Count.HasValue:
                    IMediaCollectionEnumerator mrce = enumerators[CollectionKey.ForFillerPreset(filler)];
                    var mrcePeekOffset = 0;
                    for (var i = 0; i < chapters.Count - 1; i++)
                    {
                        for (var j = 0; j < filler.Count.Value; j++)
                        {
                            foreach (MediaItem mediaItem in mrce.Peek(mrcePeekOffset))
                            {
                                totalDuration += DurationForMediaItem(mediaItem);
                                mrcePeekOffset++;
                            }
                        }
                    }

                    break;
                case (_, FillerMode.Duration) when filler.Duration.HasValue:
                    IMediaCollectionEnumerator e1 = enumerators[CollectionKey.ForFillerPreset(filler)];
                    var peekOffset1 = 0;
                    TimeSpan duration = filler.Duration.Value;
                    while (e1.Peek(peekOffset1).IsSome)
                    {
                        foreach (MediaItem mediaItem in e1.Peek(peekOffset1))
                        {
                            TimeSpan currentDuration = DurationForMediaItem(mediaItem);
                            duration -= currentDuration;
                            if (duration >= TimeSpan.Zero)
                            {
                                totalDuration += currentDuration;
                                peekOffset1++;
                            }
                        }

                        if (duration < TimeSpan.Zero)
                        {
                            break;
                        }
                    }

                    break;
                case (_, FillerMode.Count) when filler.Count.HasValue:
                    IMediaCollectionEnumerator e2 = enumerators[CollectionKey.ForFillerPreset(filler)];
                    var peekOffset2 = 0;
                    for (var i = 0; i < filler.Count.Value; i++)
                    {
                        foreach (MediaItem mediaItem in e2.Peek(peekOffset2))
                        {
                            totalDuration += DurationForMediaItem(mediaItem);
                            peekOffset2++;
                        }
                    }

                    break;
            }
        }

        foreach (FillerPreset padFiller in Optional(
                     allFiller.FirstOrDefault(f => f.FillerMode == FillerMode.Pad && f.PadToNearestMinute.HasValue)))
        {
            int currentMinute = (itemStartTime + totalDuration).Minute;
            // ReSharper disable once PossibleInvalidOperationException
            int targetMinute = (currentMinute + padFiller.PadToNearestMinute.Value - 1) /
                padFiller.PadToNearestMinute.Value * padFiller.PadToNearestMinute.Value;

            DateTimeOffset targetTime = itemStartTime + totalDuration - TimeSpan.FromMinutes(currentMinute) +
                                        TimeSpan.FromMinutes(targetMinute);

            return new DateTimeOffset(
                targetTime.Year,
                targetTime.Month,
                targetTime.Day,
                targetTime.Hour,
                targetTime.Minute,
                0,
                targetTime.Offset);
        }

        return itemStartTime + totalDuration;
    }

    internal List<PlayoutItem> AddFiller(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> enumerators,
        ProgramScheduleItem scheduleItem,
        PlayoutItem playoutItem,
        List<MediaChapter> chapters)
    {
        var result = new List<PlayoutItem>();

        var allFiller = Optional(scheduleItem.PreRollFiller)
            .Append(Optional(scheduleItem.MidRollFiller))
            .Append(Optional(scheduleItem.PostRollFiller))
            .ToList();

        // multiple pad-to-nearest-minute values are invalid; use no filler
        if (allFiller.Count(f => f.FillerMode == FillerMode.Pad && f.PadToNearestMinute.HasValue) > 1)
        {
            _logger.LogError("Multiple pad-to-nearest-minute values are invalid; no filler will be used");
            return new List<PlayoutItem> { playoutItem };
        }

        List<MediaChapter> effectiveChapters = chapters;
        if (allFiller.All(fp => fp.FillerKind != FillerKind.MidRoll) || effectiveChapters.Count <= 1)
        {
            effectiveChapters = new List<MediaChapter>();
        }

        foreach (FillerPreset filler in allFiller.Filter(
                     f => f.FillerKind == FillerKind.PreRoll && f.FillerMode != FillerMode.Pad))
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
                            FillerKind.PreRoll,
                            filler.AllowWatermarks));
                    break;
                case FillerMode.Count when filler.Count.HasValue:
                    IMediaCollectionEnumerator e2 = enumerators[CollectionKey.ForFillerPreset(filler)];
                    result.AddRange(
                        AddCountFiller(
                            playoutBuilderState,
                            e2,
                            filler.Count.Value,
                            FillerKind.PreRoll,
                            filler.AllowWatermarks));
                    break;
            }
        }

        if (effectiveChapters.Count <= 1)
        {
            result.Add(playoutItem);
        }
        else
        {
            foreach (FillerPreset filler in allFiller.Filter(
                         f => f.FillerKind == FillerKind.MidRoll && f.FillerMode != FillerMode.Pad))
            {
                switch (filler.FillerMode)
                {
                    case FillerMode.Duration when filler.Duration.HasValue:
                        IMediaCollectionEnumerator e1 = enumerators[CollectionKey.ForFillerPreset(filler)];
                        for (var i = 0; i < effectiveChapters.Count; i++)
                        {
                            result.Add(playoutItem.ForChapter(effectiveChapters[i]));
                            if (i < effectiveChapters.Count - 1)
                            {
                                result.AddRange(
                                    AddDurationFiller(
                                        playoutBuilderState,
                                        e1,
                                        filler.Duration.Value,
                                        FillerKind.MidRoll,
                                        filler.AllowWatermarks));
                            }
                        }

                        break;
                    case FillerMode.Count when filler.Count.HasValue:
                        IMediaCollectionEnumerator e2 = enumerators[CollectionKey.ForFillerPreset(filler)];
                        for (var i = 0; i < effectiveChapters.Count; i++)
                        {
                            result.Add(playoutItem.ForChapter(effectiveChapters[i]));
                            if (i < effectiveChapters.Count - 1)
                            {
                                result.AddRange(
                                    AddCountFiller(
                                        playoutBuilderState,
                                        e2,
                                        filler.Count.Value,
                                        FillerKind.MidRoll,
                                        filler.AllowWatermarks));
                            }
                        }

                        break;
                }
            }
        }

        foreach (FillerPreset filler in allFiller.Filter(
                     f => f.FillerKind == FillerKind.PostRoll && f.FillerMode != FillerMode.Pad))
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
                            FillerKind.PostRoll,
                            filler.AllowWatermarks));
                    break;
                case FillerMode.Count when filler.Count.HasValue:
                    IMediaCollectionEnumerator e2 = enumerators[CollectionKey.ForFillerPreset(filler)];
                    result.AddRange(
                        AddCountFiller(
                            playoutBuilderState,
                            e2,
                            filler.Count.Value,
                            FillerKind.PostRoll,
                            filler.AllowWatermarks));
                    break;
            }
        }

        // after all non-padded filler has been added, figure out padding
        foreach (FillerPreset padFiller in Optional(
                     allFiller.FirstOrDefault(f => f.FillerMode == FillerMode.Pad && f.PadToNearestMinute.HasValue)))
        {
            var totalDuration =
                TimeSpan.FromMilliseconds(
                    result.Sum(pi => (pi.Finish - pi.Start).TotalMilliseconds) +
                    effectiveChapters.Sum(c => (c.EndTime - c.StartTime).TotalMilliseconds));

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

            TimeSpan remainingToFill = targetTime - totalDuration - playoutItem.StartOffset;

            // _logger.LogInformation(
            //     "Total duration {TotalDuration}; need to fill {TimeSpan} to pad properly to {TargetTime}",
            //     totalDuration,
            //     remainingToFill,
            //     targetTime);

            switch (padFiller.FillerKind)
            {
                case FillerKind.PreRoll:
                    IMediaCollectionEnumerator pre1 = enumerators[CollectionKey.ForFillerPreset(padFiller)];
                    result.InsertRange(
                        0,
                        AddDurationFiller(
                            playoutBuilderState,
                            pre1,
                            remainingToFill,
                            FillerKind.PreRoll,
                            padFiller.AllowWatermarks));
                    totalDuration =
                        TimeSpan.FromMilliseconds(result.Sum(pi => (pi.Finish - pi.Start).TotalMilliseconds));
                    remainingToFill = targetTime - totalDuration - playoutItem.StartOffset;
                    if (remainingToFill > TimeSpan.Zero)
                    {
                        result.InsertRange(
                            0,
                            FallbackFillerForPad(
                                playoutBuilderState,
                                enumerators,
                                scheduleItem,
                                remainingToFill));
                    }

                    break;
                case FillerKind.MidRoll:
                    IMediaCollectionEnumerator mid1 = enumerators[CollectionKey.ForFillerPreset(padFiller)];
                    var fillerQueue = new Queue<PlayoutItem>(
                        AddDurationFiller(
                            playoutBuilderState,
                            mid1,
                            remainingToFill,
                            FillerKind.MidRoll,
                            padFiller.AllowWatermarks));
                    TimeSpan average = effectiveChapters.Count == 0
                        ? remainingToFill
                        : remainingToFill / (effectiveChapters.Count - 1);
                    TimeSpan filled = TimeSpan.Zero;
                    for (var i = 0; i < effectiveChapters.Count; i++)
                    {
                        result.Add(playoutItem.ForChapter(effectiveChapters[i]));
                        if (i < effectiveChapters.Count - 1)
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
                                        i < effectiveChapters.Count - 1 ? maxThisBreak : leftOverall);

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

                    break;
                case FillerKind.PostRoll:
                    IMediaCollectionEnumerator post1 = enumerators[CollectionKey.ForFillerPreset(padFiller)];
                    result.AddRange(
                        AddDurationFiller(
                            playoutBuilderState,
                            post1,
                            remainingToFill,
                            FillerKind.PostRoll,
                            padFiller.AllowWatermarks));
                    totalDuration =
                        TimeSpan.FromMilliseconds(result.Sum(pi => (pi.Finish - pi.Start).TotalMilliseconds));
                    remainingToFill = targetTime - totalDuration - playoutItem.StartOffset;
                    if (remainingToFill > TimeSpan.Zero)
                    {
                        result.AddRange(
                            FallbackFillerForPad(
                                playoutBuilderState,
                                enumerators,
                                scheduleItem,
                                remainingToFill));
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
        bool allowWatermarks)
    {
        var result = new List<PlayoutItem>();

        for (var i = 0; i < count; i++)
        {
            foreach (MediaItem mediaItem in enumerator.Current)
            {
                TimeSpan itemDuration = DurationForMediaItem(mediaItem);

                var playoutItem = new PlayoutItem
                {
                    MediaItemId = mediaItem.Id,
                    Start = new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Finish = new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc) + itemDuration,
                    InPoint = TimeSpan.Zero,
                    OutPoint = itemDuration,
                    GuideGroup = playoutBuilderState.NextGuideGroup,
                    FillerKind = fillerKind,
                    DisableWatermarks = !allowWatermarks
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
        bool allowWatermarks)
    {
        var result = new List<PlayoutItem>();

        TimeSpan remainingToFill = duration;
        var skipped = false;
        while (enumerator.Current.IsSome && remainingToFill > TimeSpan.Zero)
        {
            foreach (MediaItem mediaItem in enumerator.Current)
            {
                TimeSpan itemDuration = DurationForMediaItem(mediaItem);

                if (remainingToFill - itemDuration >= TimeSpan.Zero)
                {
                    var playoutItem = new PlayoutItem
                    {
                        MediaItemId = mediaItem.Id,
                        Start = new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                        Finish = new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc) + itemDuration,
                        InPoint = TimeSpan.Zero,
                        OutPoint = itemDuration,
                        GuideGroup = playoutBuilderState.NextGuideGroup,
                        FillerKind = fillerKind,
                        DisableWatermarks = !allowWatermarks
                    };

                    remainingToFill -= itemDuration;
                    result.Add(playoutItem);
                    enumerator.MoveNext();
                }
                else if (skipped)
                {
                    // set to zero so it breaks out of the while loop
                    remainingToFill = TimeSpan.Zero;
                }
                else
                {
                    if (itemDuration >= duration * 1.5)
                    {
                        _logger.LogWarning(
                            "Filler item is too long {FillerDuration} to fill {GapDuration}; skipping to next filler item",
                            itemDuration,
                            duration);

                        skipped = true;
                        enumerator.MoveNext();
                    }
                    else
                    {
                        if (itemDuration > duration)
                        {
                            _logger.LogWarning(
                                "Filler item is too long {FillerDuration} to fill {GapDuration}; aborting filler block",
                                itemDuration,
                                duration);
                        }

                        // set to zero so it breaks out of the while loop
                        remainingToFill = TimeSpan.Zero;
                    }
                }
            }
        }

        return result;
    }

    private static Option<PlayoutItem> FallbackFillerForPad(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> enumerators,
        ProgramScheduleItem scheduleItem,
        TimeSpan duration)
    {
        if (scheduleItem.FallbackFiller != null)
        {
            IMediaCollectionEnumerator enumerator =
                enumerators[CollectionKey.ForFillerPreset(scheduleItem.FallbackFiller)];

            foreach (MediaItem mediaItem in enumerator.Current)
            {
                var result = new PlayoutItem
                {
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
}
