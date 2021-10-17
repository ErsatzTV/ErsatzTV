using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class SchedulerOne : SchedulerBase
    {
        protected override Tuple<PlayoutBuilderState, List<PlayoutItem>> ScheduleImpl(
            PlayoutBuilderState playoutBuilderState,
            Map<CollectionKey, List<MediaItem>> collectionMediaItems,
            ProgramScheduleItem scheduleItem,
            MediaItem mediaItem,
            MediaVersion version,
            DateTimeOffset itemStartTime,
            ILogger logger)
        {
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

            return Tuple(nextState, new List<PlayoutItem> { playoutItem });
        }
    }
}
