using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules;

public record ReplaceProgramScheduleItem(
    int Index,
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
    string PreferredAudioTitle,
    string PreferredSubtitleLanguageCode,
    ChannelSubtitleMode? SubtitleMode) : IProgramScheduleItemRequest;

public record ReplaceProgramScheduleItems
    (int ProgramScheduleId, List<ReplaceProgramScheduleItem> Items) : IRequest<
        Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>>;
