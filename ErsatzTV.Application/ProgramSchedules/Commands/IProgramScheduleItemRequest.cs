using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Application.ProgramSchedules;

public interface IProgramScheduleItemRequest
{
    TimeSpan? StartTime { get; }
    FixedStartTimeBehavior? FixedStartTimeBehavior { get; }
    CollectionType CollectionType { get; }
    int? CollectionId { get; }
    int? MultiCollectionId { get; }
    int? SmartCollectionId { get; }
    int? RerunCollectionId { get; }
    int? MediaItemId { get; }
    int? PlaylistId { get; }
    string SearchTitle { get; }
    string SearchQuery { get; }
    PlayoutMode PlayoutMode { get; }
    PlaybackOrder PlaybackOrder { get; }
    MarathonGroupBy MarathonGroupBy { get; }
    bool MarathonShuffleGroups { get; }
    bool MarathonShuffleItems { get; }
    int? MarathonBatchSize { get; }
    FillWithGroupMode FillWithGroupMode { get; }
    MultipleMode MultipleMode { get; }
    int? MultipleCount { get; }
    TimeSpan? PlayoutDuration { get; }
    TailMode TailMode { get; }
    int? DiscardToFillAttempts { get; }
    string CustomTitle { get; }
    GuideMode GuideMode { get; }
    int? PreRollFillerId { get; }
    int? MidRollFillerId { get; }
    int? PostRollFillerId { get; }
    int? TailFillerId { get; }
    int? FallbackFillerId { get; }
    List<int> WatermarkIds { get; }
    List<int> GraphicsElementIds { get; }
    string PreferredAudioLanguageCode { get; }
    string PreferredAudioTitle { get; }
    string PreferredSubtitleLanguageCode { get; }
    ChannelSubtitleMode? SubtitleMode { get; }
}
