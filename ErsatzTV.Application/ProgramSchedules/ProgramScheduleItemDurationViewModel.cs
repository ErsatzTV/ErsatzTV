using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules
{
    public record ProgramScheduleItemDurationViewModel : ProgramScheduleItemViewModel
    {
        public ProgramScheduleItemDurationViewModel(
            int id,
            int index,
            StartType startType,
            TimeSpan? startTime,
            MediaCollectionViewModel mediaCollection,
            TimeSpan playoutDuration,
            bool offlineTail) : base(
            id,
            index,
            startType,
            startTime,
            PlayoutMode.Duration,
            mediaCollection)
        {
            PlayoutDuration = playoutDuration;
            OfflineTail = offlineTail;
        }

        public TimeSpan PlayoutDuration { get; }
        public bool OfflineTail { get; }
    }
}
