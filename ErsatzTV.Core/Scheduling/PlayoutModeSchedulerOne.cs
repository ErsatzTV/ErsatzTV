using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class PlayoutModeSchedulerOne : PlayoutModeSchedulerBase<ProgramScheduleItemOne>
    {
        public override Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            ProgramScheduleItemOne scheduleItem,
            ProgramScheduleItem nextScheduleItem,
            DateTimeOffset hardStop,
            ILogger logger)
        {
            IMediaCollectionEnumerator contentEnumerator =
                collectionEnumerators[CollectionKey.ForScheduleItem(scheduleItem)];
            foreach (MediaItem mediaItem in contentEnumerator.Current)
            {
                // find when we should start this item, based on the current time
                DateTimeOffset itemStartTime = GetStartTimeAfter(
                    playoutBuilderState,
                    scheduleItem);

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
                    CustomGroup = false,
                    IsFiller = scheduleItem.GuideMode == GuideMode.Filler
                };
                
                // only play one item from collection, so always advance to the next item
                logger.LogDebug(
                    "Advancing to next schedule item after playout mode {PlayoutMode}",
                    "One");

                PlayoutBuilderState nextState = playoutBuilderState with
                {
                    CurrentTime = itemStartTime + version.Duration,
                    ScheduleItemIndex = playoutBuilderState.ScheduleItemIndex + 1,
                    CustomGroup = false
                };

                contentEnumerator.MoveNext();

                List<PlayoutItem> playoutItems = new List<PlayoutItem> { playoutItem };

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
            
            return Tuple(playoutBuilderState, new List<PlayoutItem>());
        }
    }
}
