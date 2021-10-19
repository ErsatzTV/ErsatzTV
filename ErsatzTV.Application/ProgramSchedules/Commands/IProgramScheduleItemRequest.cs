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
        int? SmartCollectionId { get; }
        int? MediaItemId { get; }
        PlayoutMode PlayoutMode { get; }
        PlaybackOrder PlaybackOrder { get; }
        int? MultipleCount { get; }
        TimeSpan? PlayoutDuration { get; }
        TailMode TailMode { get; }
        ProgramScheduleItemCollectionType TailCollectionType { get; }
        int? TailCollectionId { get; }
        int? TailMultiCollectionId { get; }
        int? TailSmartCollectionId { get; }
        int? TailMediaItemId { get; }
        string CustomTitle { get; }
        GuideMode GuideMode { get; }
        int? PreRollFillerId { get; }
        int? MidRollFillerId { get; }
        int? PostRollFillerId { get; }
        int? TailFillerId { get; }
        int? FallbackFillerId { get; }
    }
}
