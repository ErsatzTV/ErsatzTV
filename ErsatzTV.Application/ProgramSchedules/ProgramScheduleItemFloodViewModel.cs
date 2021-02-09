using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules
{
    public record ProgramScheduleItemFloodViewModel : ProgramScheduleItemViewModel
    {
        public ProgramScheduleItemFloodViewModel(
            int id,
            int index,
            StartType startType,
            TimeSpan? startTime,
            MediaCollectionViewModel mediaCollection) : base(
            id,
            index,
            startType,
            startTime,
            PlayoutMode.Flood,
            mediaCollection)
        {
        }
    }
}
