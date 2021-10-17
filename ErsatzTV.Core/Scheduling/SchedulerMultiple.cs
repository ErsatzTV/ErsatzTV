using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class SchedulerMultiple : SchedulerBase
    {
        protected override Tuple<PlayoutBuilderState, PlayoutItem> ScheduleImpl(
            PlayoutBuilderState playoutBuilderState,
            Map<CollectionKey, List<MediaItem>> collectionMediaItems,
            ProgramScheduleItem scheduleItem,
            MediaItem mediaItem,
            MediaVersion version,
            DateTimeOffset itemStartTime,
            ILogger logger)
        {
            PlayoutBuilderState nextState = playoutBuilderState;
            var playoutItem = new PlayoutItem
            {
                MediaItemId = mediaItem.Id,
                Start = itemStartTime.UtcDateTime,
                Finish = itemStartTime.UtcDateTime + version.Duration,
                CustomGroup = nextState.CustomGroup,
                IsFiller = scheduleItem.GuideMode == GuideMode.Filler
            };
            
            if (nextState.MultipleRemaining.IsNone && scheduleItem is ProgramScheduleItemMultiple multiple)
            {
                if (multiple.Count == 0)
                {
                    nextState = nextState with
                    {
                        MultipleRemaining = collectionMediaItems[CollectionKeyForItem(scheduleItem)].Count
                    };
                }
                else
                {
                    nextState = nextState with
                    {
                        MultipleRemaining = multiple.Count
                    };
                }

                nextState = nextState with { CustomGroup = true };
            }

            nextState = nextState with
            {
                MultipleRemaining = nextState.MultipleRemaining.Map(i => i - 1)
            };
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

            nextState = nextState with
            {
                CurrentTime = itemStartTime + version.Duration
            };
            
            return Tuple(nextState, playoutItem);
        }
    }
}
