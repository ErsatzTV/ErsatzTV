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

                    MediaVersion version = mediaItem switch
                    {
                        Movie m => m.MediaVersions.Head(),
                        Episode e => e.MediaVersions.Head(),
                        MusicVideo mv => mv.MediaVersions.Head(),
                        OtherVideo mv => mv.MediaVersions.Head(),
                        _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
                    };

                    if (nextState.CurrentTime + version.Duration > nextItemStart)
                    {
                        _logger.LogDebug(
                            "Filler with duration {Duration} will go past next item start {NextItemStart}",
                            version.Duration,
                            nextItemStart);

                        break;
                    }
                    
                    var playoutItem = new PlayoutItem
                    {
                        MediaItemId = mediaItem.Id,
                        Start = nextState.CurrentTime.UtcDateTime,
                        Finish = nextState.CurrentTime.UtcDateTime + version.Duration,
                        FillerKind = FillerKind.Tail,
                        CustomGroup = true
                    };

                    newItems.Add(playoutItem);

                    nextState = nextState with
                    {
                        CurrentTime = nextState.CurrentTime + version.Duration
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
                        CustomGroup = true,
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
            TimeSpan itemDuration)
        {
            var allFiller = Optional(scheduleItem.PreRollFiller)
                .Append(Optional(scheduleItem.MidRollFiller))
                .Append(Optional(scheduleItem.PostRollFiller))
                .ToList();

            if (allFiller.Map(f => Optional(f.PadToNearestMinute)).Sequence().Flatten().Distinct().Count() > 1)
            {
                // multiple pad-to-nearest-minute values are invalid; use no filler
                return itemStartTime + itemDuration;
            }

            TimeSpan totalDuration = itemDuration;
            foreach (FillerPreset filler in allFiller)
            {
                switch (filler.FillerMode)
                {
                    case FillerMode.Duration when filler.Duration.HasValue:
                        totalDuration += filler.Duration.Value;
                        break;
                    case FillerMode.Multiple when filler.Count.HasValue:
                        IMediaCollectionEnumerator enumerator =
                            enumerators[CollectionKey.ForFillerPreset(filler)].Clone();
                        for (int i = 0; i < filler.Count.Value; i++)
                        {
                            foreach (MediaItem mediaItem in enumerator.Current)
                            {
                                totalDuration += DurationForMediaItem(mediaItem);
                                enumerator.MoveNext();
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

        protected List<PlayoutItem> AddFiller(PlayoutItem playoutItem)
        {
            return new List<PlayoutItem> { playoutItem };
        }
    }
}
