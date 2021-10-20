using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public abstract class PlayoutModeSchedulerBase<T> : IPlayoutModeScheduler<T> where T : ProgramScheduleItem
    {
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
            DateTimeOffset hardStop,
            ILogger logger);

        protected Tuple<PlayoutBuilderState, List<PlayoutItem>> AddTailFiller(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            ProgramScheduleItem scheduleItem,
            List<PlayoutItem> playoutItems,
            DateTimeOffset nextItemStart,
            ILogger logger)
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
                        logger.LogDebug(
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
            DateTimeOffset nextItemStart,
            ILogger logger)
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

        protected TimeSpan DurationForMediaItem(MediaItem mediaItem)
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
    }
}
