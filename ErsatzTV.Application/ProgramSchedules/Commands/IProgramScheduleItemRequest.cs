using System;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public interface IProgramScheduleItemRequest
    {
        TimeSpan? StartTime { get; }
        int MediaCollectionId { get; }
        PlayoutMode PlayoutMode { get; }
        int? MultipleCount { get; }
        TimeSpan? PlayoutDuration { get; }
        bool? OfflineTail { get; }
    }
}
