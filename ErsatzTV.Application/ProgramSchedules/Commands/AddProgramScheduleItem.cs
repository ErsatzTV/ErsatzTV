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
    int? MidRollFillerId,
    int? PostRollFillerId,
    int? TailFillerId,
    int? FallbackFillerId,
    int? WatermarkId,
    string PreferredAudioLanguageCode,
    string PreferredSubtitleLanguageCode,
    ChannelSubtitleMode? SubtitleMode) : IRequest<Either<BaseError, ProgramScheduleItemViewModel>>,
    IProgramScheduleItemRequest;
