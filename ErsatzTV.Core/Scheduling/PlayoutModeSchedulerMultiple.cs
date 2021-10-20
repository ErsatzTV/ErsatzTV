using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class PlayoutModeSchedulerMultiple : PlayoutModeSchedulerBase<ProgramScheduleItemMultiple>
    {
        private readonly Map<CollectionKey, List<MediaItem>> _collectionMediaItems;

        public PlayoutModeSchedulerMultiple(Map<CollectionKey,List<MediaItem>> collectionMediaItems)
        {
            _collectionMediaItems = collectionMediaItems;
        }

        public override Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            ProgramScheduleItemMultiple scheduleItem,
            ProgramScheduleItem nextScheduleItem,
            DateTimeOffset hardStop,
            ILogger logger)
        {
            var playoutItems = new List<PlayoutItem>();
            
            PlayoutBuilderState nextState = playoutBuilderState with
            {
                MultipleRemaining = playoutBuilderState.MultipleRemaining.IfNone(scheduleItem.Count)
            };

            if (nextState.MultipleRemaining == 0)
            {
                nextState = nextState with
                {
                    MultipleRemaining = _collectionMediaItems[CollectionKey.ForScheduleItem(scheduleItem)].Count
                };
            }

            IMediaCollectionEnumerator contentEnumerator =
                collectionEnumerators[CollectionKey.ForScheduleItem(scheduleItem)];
            while (contentEnumerator.Current.IsSome && nextState.MultipleRemaining > 0 &&
                   nextState.CurrentTime < hardStop)
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

                playoutItems.Add(playoutItem);
                
                nextState = nextState with
                {
                    CurrentTime = itemStartTime + version.Duration,
                    MultipleRemaining = nextState.MultipleRemaining.Map(i => i - 1)
                };

                contentEnumerator.MoveNext();
            }

            if (nextState.MultipleRemaining.IfNone(-1) == 0)
            {
                logger.LogDebug(
                    "Advancing to next schedule item after playout mode {PlayoutMode}",
                    "Multiple");

                nextState = nextState with
                {
                    ScheduleItemIndex = nextState.ScheduleItemIndex + 1,
                    MultipleRemaining = None,
                    CustomGroup = false
                };
            }
            
            DateTimeOffset nextItemStart = GetStartTimeAfter(nextState, nextScheduleItem);

            if (scheduleItem.TailFiller != null)
            {
                (nextState, playoutItems) = AddTailFiller(
                    nextState,
                    collectionEnumerators,
                    scheduleItem,
                    playoutItems,
                    nextItemStart,
                    logger);
            }

            if (scheduleItem.FallbackFiller != null)
            {
                (nextState, playoutItems) = AddFallbackFiller(
                    nextState,
                    collectionEnumerators,
                    scheduleItem,
                    playoutItems,
                    nextItemStart,
                    logger);
            }

            return Tuple(nextState, playoutItems);
        }
    }
}
