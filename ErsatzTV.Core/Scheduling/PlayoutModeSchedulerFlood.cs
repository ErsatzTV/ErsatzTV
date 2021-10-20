using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class PlayoutModeSchedulerFlood : PlayoutModeSchedulerBase<ProgramScheduleItemFlood>
    {
        private readonly List<ProgramScheduleItem> _sortedScheduleItems;

        public PlayoutModeSchedulerFlood(List<ProgramScheduleItem> sortedScheduleItems)
        {
            _sortedScheduleItems = sortedScheduleItems;
        }

        public override Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            ProgramScheduleItemFlood scheduleItem,
            ProgramScheduleItem nextScheduleItem,
            DateTimeOffset hardStop,
            ILogger logger)
        {
            var playoutItems = new List<PlayoutItem>();

            PlayoutBuilderState nextState = playoutBuilderState;
            var willFinishInTime = true;

            IMediaCollectionEnumerator contentEnumerator =
                collectionEnumerators[CollectionKey.ForScheduleItem(scheduleItem)];
            while (contentEnumerator.Current.IsSome && nextState.CurrentTime < hardStop && willFinishInTime)
            {
                MediaItem mediaItem = contentEnumerator.Current.ValueUnsafe();

                // find when we should start this item, based on the current time
                DateTimeOffset itemStartTime = GetStartTimeAfter(nextState, scheduleItem);

                MediaVersion version = mediaItem switch
                {
                    Movie m => m.MediaVersions.Head(),
                    Episode e => e.MediaVersions.Head(),
                    MusicVideo mv => mv.MediaVersions.Head(),
                    OtherVideo mv => mv.MediaVersions.Head(),
                    _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
                };
                
                var playoutItem = new PlayoutItem
                {
                    MediaItemId = mediaItem.Id,
                    Start = itemStartTime.UtcDateTime,
                    Finish = itemStartTime.UtcDateTime + version.Duration,
                    CustomGroup = true,
                    IsFiller = scheduleItem.GuideMode == GuideMode.Filler
                };
                
                ProgramScheduleItem peekScheduleItem =
                    _sortedScheduleItems[(nextState.ScheduleItemIndex + 1) % _sortedScheduleItems.Count];
                DateTimeOffset peekScheduleItemStart =
                    peekScheduleItem.StartType == StartType.Fixed
                        ? GetStartTimeAfter(nextState, peekScheduleItem)
                        : DateTimeOffset.MaxValue;
                
                // if the current time is before the next schedule item, but the current finish
                // is after, we need to move on to the next schedule item
                // eventually, spots probably have to fit in this gap
                willFinishInTime = itemStartTime > peekScheduleItemStart ||
                                   itemStartTime + version.Duration <= peekScheduleItemStart;

                if (willFinishInTime)
                {
                    logger.LogDebug(
                        "Scheduling media item: {ScheduleItemNumber} / {CollectionType} / {MediaItemId} - {MediaItemTitle} / {StartTime}",
                        scheduleItem.Index,
                        scheduleItem.CollectionType,
                        mediaItem.Id,
                        PlayoutBuilder.DisplayTitle(mediaItem),
                        itemStartTime);

                    playoutItems.Add(playoutItem);

                    nextState = nextState with
                    {
                        CurrentTime = itemStartTime + version.Duration,
                        InFlood = true
                    };

                    contentEnumerator.MoveNext();
                }
            }

            logger.LogDebug(
                "Advancing to next schedule item after playout mode {PlayoutMode}",
                "Flood");

            nextState = nextState with
            {
                ScheduleItemIndex = nextState.ScheduleItemIndex + 1,
                InFlood = nextState.CurrentTime >= hardStop,
                CustomGroup = false
            };
            
            ProgramScheduleItem peekItem =
                _sortedScheduleItems[nextState.ScheduleItemIndex % _sortedScheduleItems.Count];
            DateTimeOffset peekItemStart = GetStartTimeAfter(nextState, peekItem);

            if (scheduleItem.TailFiller != null)
            {
                (nextState, playoutItems) = AddTailFiller(
                    nextState,
                    collectionEnumerators,
                    scheduleItem,
                    playoutItems,
                    peekItemStart,
                    logger);
            }

            if (scheduleItem.FallbackFiller != null)
            {
                (nextState, playoutItems) = AddFallbackFiller(
                    nextState,
                    collectionEnumerators,
                    scheduleItem,
                    playoutItems,
                    peekItemStart,
                    logger);
            }

            return Tuple(nextState, playoutItems);
        }
    }
}
