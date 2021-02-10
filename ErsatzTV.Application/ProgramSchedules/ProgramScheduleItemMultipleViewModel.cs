using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules
{
    public record ProgramScheduleItemMultipleViewModel : ProgramScheduleItemViewModel
    {
        public ProgramScheduleItemMultipleViewModel(
            int id,
            int index,
            StartType startType,
            TimeSpan? startTime,
            MediaCollectionViewModel mediaCollection,
            int count) : base(
            id,
            index,
            startType,
            startTime,
            PlayoutMode.Multiple,
            mediaCollection) =>
            Count = count;

        public int Count { get; }
    }
}
