using System;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public interface IProgramScheduleItemRequest
    {
        TimeSpan? StartTime { get; }
        ProgramScheduleItemCollectionType CollectionType { get; }
        int? CollectionId { get; }
        int? MultiCollectionId { get; }
        int? MediaItemId { get; }
        PlayoutMode PlayoutMode { get; }
        PlaybackOrder PlaybackOrder { get; }
        int? MultipleCount { get; }
        TimeSpan? PlayoutDuration { get; }
        bool? OfflineTail { get; }
        string CustomTitle { get; }
    }
}
