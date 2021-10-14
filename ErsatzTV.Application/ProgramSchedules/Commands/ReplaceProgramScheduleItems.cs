using System;
using System.Collections.Generic;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
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
        ProgramScheduleItemCollectionType TailCollectionType,
        int? TailCollectionId,
        int? TailMultiCollectionId,
        int? TailSmartCollectionId,
        int? TailMediaItemId,
        string CustomTitle,
        GuideMode GuideMode) : IProgramScheduleItemRequest;

    public record ReplaceProgramScheduleItems
        (int ProgramScheduleId, List<ReplaceProgramScheduleItem> Items) : IRequest<
            Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>>;
}
