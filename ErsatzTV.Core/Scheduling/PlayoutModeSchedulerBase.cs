using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using LanguageExt;

namespace ErsatzTV.Core.Scheduling
{
    public abstract class PlayoutModeSchedulerBase<T> : IPlayoutModeScheduler<T> where T : ProgramScheduleItem
    {
        protected readonly ILogger _logger;

        protected PlayoutModeSchedulerBase(ILogger logger)
        {
            _logger = logger;
        }

        public static DateTimeOffset GetStartTimeAfter(
            PlayoutBuilderState state,
            ProgramScheduleItem scheduleItem)
        {
            DateTimeOffset startTime = state.CurrentTime;

            bool isIncomplete = scheduleItem is ProgramScheduleItemMultiple && state.MultipleRemaining.IsSome ||
                                scheduleItem is ProgramScheduleItemDuration && state.DurationFinish.IsSome ||
                                scheduleItem is ProgramScheduleItemFlood && state.InFlood ||
                                scheduleItem is ProgramScheduleItemDuration && state.InDurationFiller;
            
            if (scheduleItem.StartType == StartType.Fixed && !isIncomplete)
            {
                TimeSpan itemStartTime = scheduleItem.StartTime.GetValueOrDefault();
                DateTimeOffset result = startTime.Date + itemStartTime;
                // need to wrap to the next day if appropriate
                startTime = startTime.TimeOfDay > itemStartTime ? result.AddDays(1) : result;
            }

            return startTime;
        }

        public abstract Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            T scheduleItem,
            ProgramScheduleItem nextScheduleItem,
            DateTimeOffset hardStop);

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
                        GuideGroup = nextState.NextGuideGroup
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
                        FillerKind = FillerKind.Fallback
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
            MediaVersion version = mediaItem switch
            {
                Movie m => m.MediaVersions.Head(),
                Episode e => e.MediaVersions.Head(),
                MusicVideo mv => mv.MediaVersions.Head(),
                OtherVideo mv => mv.MediaVersions.Head(),
                _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
            };

            return version.Duration;
        }

        protected static List<MediaChapter> ChaptersForMediaItem(MediaItem mediaItem)
        {
            MediaVersion version = mediaItem switch
            {
                Movie m => m.MediaVersions.Head(),
                Episode e => e.MediaVersions.Head(),
                MusicVideo mv => mv.MediaVersions.Head(),
                OtherVideo mv => mv.MediaVersions.Head(),
                _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
            };

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

            if (allFiller.Map(f => Optional(f.PadToNearestMinute)).Sequence().Flatten().Distinct().Count() > 1)
            {
                // multiple pad-to-nearest-minute values are invalid; use no filler
                // TODO: log error?
                return itemStartTime + itemDuration;
            }

            TimeSpan totalDuration = itemDuration;
            foreach (FillerPreset filler in allFiller)
            {
                switch (filler.FillerKind, filler.FillerMode)
                {
                    case (FillerKind.MidRoll, FillerMode.Duration) when filler.Duration.HasValue:
                        IMediaCollectionEnumerator mrde = enumerators[CollectionKey.ForFillerPreset(filler)].Clone();
                        for (var i = 0; i < chapters.Count - 1; i++)
                        {
                            TimeSpan midRollDuration = filler.Duration.Value;
                            while (mrde.Current.IsSome)
                            {
                                foreach (MediaItem mediaItem in mrde.Current)
                                {
                                    TimeSpan currentDuration = DurationForMediaItem(mediaItem);
                                    midRollDuration -= currentDuration;
                                    if (midRollDuration >= TimeSpan.Zero)
                                    {
                                        totalDuration += currentDuration;
                                        mrde.MoveNext();
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
                        IMediaCollectionEnumerator mrce = enumerators[CollectionKey.ForFillerPreset(filler)].Clone();
                        for (var i = 0; i < chapters.Count - 1; i++)
                        {
                            for (var j = 0; j < filler.Count.Value; j++)
                            {
                                foreach (MediaItem mediaItem in mrce.Current)
                                {
                                    totalDuration += DurationForMediaItem(mediaItem);
                                    mrce.MoveNext();
                                }
                            }
                        }

                        break;
                    case (_, FillerMode.Duration) when filler.Duration.HasValue:
                        IMediaCollectionEnumerator e1 = enumerators[CollectionKey.ForFillerPreset(filler)].Clone();
                        TimeSpan duration = filler.Duration.Value;
                        while (e1.Current.IsSome)
                        {
                            foreach (MediaItem mediaItem in e1.Current)
                            {
                                TimeSpan currentDuration = DurationForMediaItem(mediaItem);
                                duration -= currentDuration;
                                if (duration >= TimeSpan.Zero)
                                {
                                    totalDuration += currentDuration;
                                    e1.MoveNext();
                                }
                            }

                            if (duration < TimeSpan.Zero)
                            {
                                break;
                            }
                        }
                        break;
                    case (_, FillerMode.Count) when filler.Count.HasValue:
                        IMediaCollectionEnumerator e2 = enumerators[CollectionKey.ForFillerPreset(filler)].Clone();
                        for (var i = 0; i < filler.Count.Value; i++)
                        {
                            foreach (MediaItem mediaItem in e2.Current)
                            {
                                totalDuration += DurationForMediaItem(mediaItem);
                                e2.MoveNext();
                            }
                        }

                        break;
                }
            }

            foreach (FillerPreset padFiller in Optional(allFiller.FirstOrDefault(f => f.PadToNearestMinute.HasValue)))
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

        protected List<PlayoutItem> AddFiller(
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

            if (allFiller.Map(f => Optional(f.PadToNearestMinute)).Sequence().Flatten().Distinct().Count() > 1)
            {
                // multiple pad-to-nearest-minute values are invalid; use no filler
                // TODO: log error?
                return new List<PlayoutItem> { playoutItem };
            }
            
            foreach (FillerPreset filler in allFiller.Filter(f => f.FillerKind == FillerKind.PreRoll))
            {
                switch (filler.FillerMode)
                {
                    case FillerMode.Duration when filler.Duration.HasValue:
                        IMediaCollectionEnumerator e1 = enumerators[CollectionKey.ForFillerPreset(filler)];
                        result.AddRange(
                            AddDurationFiller(playoutBuilderState, e1, filler.Duration.Value, FillerKind.PreRoll));
                        break;
                    case FillerMode.Count when filler.Count.HasValue:
                        IMediaCollectionEnumerator e2 = enumerators[CollectionKey.ForFillerPreset(filler)];
                        result.AddRange(
                            AddCountFiller(playoutBuilderState, e2, filler.Count.Value, FillerKind.PreRoll));
                        break;
                }
            }
            
            if (allFiller.All(f => f.FillerKind != FillerKind.MidRoll) || !chapters.Any())
            {
                result.Add(playoutItem);
            }
            else
            {
                foreach (FillerPreset filler in allFiller.Filter(f => f.FillerKind == FillerKind.MidRoll))
                {
                    switch (filler.FillerMode)
                    {
                        case FillerMode.Duration when filler.Duration.HasValue:
                            IMediaCollectionEnumerator e1 = enumerators[CollectionKey.ForFillerPreset(filler)];
                            for (var i = 0; i < chapters.Count; i++)
                            {
                                result.Add(playoutItem.ForChapter(chapters[i]));
                                if (i < chapters.Count - 1)
                                {
                                    result.AddRange(
                                        AddDurationFiller(
                                            playoutBuilderState,
                                            e1,
                                            filler.Duration.Value,
                                            FillerKind.MidRoll));
                                }
                            }

                            break;
                        case FillerMode.Count when filler.Count.HasValue:
                            IMediaCollectionEnumerator e2 = enumerators[CollectionKey.ForFillerPreset(filler)];
                            for (var i = 0; i < chapters.Count - 1; i++)
                            {
                                result.Add(playoutItem.ForChapter(chapters[i]));
                                result.AddRange(
                                    AddCountFiller(
                                        playoutBuilderState,
                                        e2,
                                        filler.Count.Value,
                                        FillerKind.MidRoll));
                            }

                            break;
                    }
                }
            }

            foreach (FillerPreset filler in allFiller.Filter(f => f.FillerKind == FillerKind.PostRoll))
            {
                switch (filler.FillerMode)
                {
                    case FillerMode.Duration when filler.Duration.HasValue:
                        IMediaCollectionEnumerator e1 = enumerators[CollectionKey.ForFillerPreset(filler)];
                        result.AddRange(
                            AddDurationFiller(playoutBuilderState, e1, filler.Duration.Value, FillerKind.PostRoll));
                        break;
                    case FillerMode.Count when filler.Count.HasValue:
                        IMediaCollectionEnumerator e2 = enumerators[CollectionKey.ForFillerPreset(filler)];
                        result.AddRange(
                            AddCountFiller(playoutBuilderState, e2, filler.Count.Value, FillerKind.PostRoll));
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
            FillerKind fillerKind)
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
                        Start = SystemTime.MinValueUtc,
                        Finish = SystemTime.MinValueUtc + itemDuration,
                        InPoint = TimeSpan.Zero,
                        OutPoint = itemDuration,
                        GuideGroup = playoutBuilderState.NextGuideGroup,
                        FillerKind = fillerKind
                    };

                    result.Add(playoutItem);
                    enumerator.MoveNext();
                }
            }

            return result;
        }

        private static List<PlayoutItem> AddDurationFiller(
            PlayoutBuilderState playoutBuilderState,
            IMediaCollectionEnumerator enumerator,
            TimeSpan duration,
            FillerKind fillerKind)
        {
            var result = new List<PlayoutItem>();
            
            while (enumerator.Current.IsSome)
            {
                foreach (MediaItem mediaItem in enumerator.Current)
                {
                    // TODO: retry up to x times when item doesn't fit?
                    
                    TimeSpan itemDuration = DurationForMediaItem(mediaItem);
                    duration -= itemDuration;

                    if (duration >= TimeSpan.Zero)
                    {
                        var playoutItem = new PlayoutItem
                        {
                            MediaItemId = mediaItem.Id,
                            Start = SystemTime.MinValueUtc,
                            Finish = SystemTime.MinValueUtc + itemDuration,
                            InPoint = TimeSpan.Zero,
                            OutPoint = itemDuration,
                            GuideGroup = playoutBuilderState.NextGuideGroup,
                            FillerKind = fillerKind
                        };

                        result.Add(playoutItem);
                        enumerator.MoveNext();
                    }
                }

                if (duration < TimeSpan.Zero)
                {
                    break;
                }
            }

            return result;
        }
    }
}
