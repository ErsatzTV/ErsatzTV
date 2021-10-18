using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;

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
    }
}
