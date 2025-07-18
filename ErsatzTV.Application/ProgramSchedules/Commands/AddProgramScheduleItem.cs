using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Application.ProgramSchedules;

public record AddProgramScheduleItem(
    int ProgramScheduleId,
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
    string PreferredAudioLanguageCode,
    string PreferredAudioTitle,
    string PreferredSubtitleLanguageCode,
    ChannelSubtitleMode? SubtitleMode) : IRequest<Either<BaseError, ProgramScheduleItemViewModel>>,
    IProgramScheduleItemRequest;
