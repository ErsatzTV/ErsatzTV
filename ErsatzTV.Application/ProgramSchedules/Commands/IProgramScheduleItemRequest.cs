using System;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public interface IProgramScheduleItemRequest
    {
        TimeSpan? StartTime { get; }
        ProgramScheduleItemCollectionType CollectionType { get; }
        int? MediaCollectionId { get; }
        int? TelevisionShowId { get; }
        int? TelevisionSeasonId { get; }
        PlayoutMode PlayoutMode { get; }
        int? MultipleCount { get; }
        TimeSpan? PlayoutDuration { get; }
        bool? OfflineTail { get; }
    }
}
