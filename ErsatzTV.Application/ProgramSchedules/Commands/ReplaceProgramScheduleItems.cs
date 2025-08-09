using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Application.ProgramSchedules;

public record ReplaceProgramScheduleItem(
    int Index,
    StartType StartType,
    TimeSpan? StartTime,
    FixedStartTimeBehavior? FixedStartTimeBehavior,
    PlayoutMode PlayoutMode,
    ProgramScheduleItemCollectionType CollectionType,
    int? CollectionId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? MediaItemId,
    int? PlaylistId,
    PlaybackOrder PlaybackOrder,
    FillWithGroupMode FillWithGroupMode,
    MultipleMode MultipleMode,
    int? MultipleCount,
    TimeSpan? PlayoutDuration,
    TailMode TailMode,
    int? DiscardToFillAttempts,
    string CustomTitle,
    GuideMode GuideMode,
    int? PreRollFillerId,
    int? MidRollFillerId,
    int? PostRollFillerId,
    int? TailFillerId,
    int? FallbackFillerId,
    int? WatermarkId,
    List<int> WatermarkIds,
    string PreferredAudioLanguageCode,
    string PreferredAudioTitle,
    string PreferredSubtitleLanguageCode,
    ChannelSubtitleMode? SubtitleMode) : IProgramScheduleItemRequest;

public record ReplaceProgramScheduleItems(int ProgramScheduleId, List<ReplaceProgramScheduleItem> Items) : IRequest<
    Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>>;
