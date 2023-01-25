using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules;

public record AddProgramScheduleItem(
    int ProgramScheduleId,
    StartType StartType,
    TimeSpan? StartTime,
    PlayoutMode PlayoutMode,
    ProgramScheduleItemCollectionType CollectionType,
    int? CollectionId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? MediaItemId,
    PlaybackOrder PlaybackOrder,
    int? MultipleCount,
    TimeSpan? PlayoutDuration,
    TailMode TailMode,
    string CustomTitle,
    GuideMode GuideMode,
    int? PreRollFillerId,
    int? MidRollEnterFillerId,
    int? MidRollFillerId,
    int? MidRollExitFillerId,
    int? PostRollFillerId,
    int? TailFillerId,
    int? FallbackFillerId,
    int? WatermarkId,
    string PreferredAudioLanguageCode,
    string PreferredAudioTitle,
    string PreferredSubtitleLanguageCode,
    ChannelSubtitleMode? SubtitleMode) : IRequest<Either<BaseError, ProgramScheduleItemViewModel>>,
    IProgramScheduleItemRequest;
