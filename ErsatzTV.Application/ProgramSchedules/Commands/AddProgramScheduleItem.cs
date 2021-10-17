using System;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
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
        ProgramScheduleItemCollectionType TailCollectionType,
        int? TailCollectionId,
        int? TailMultiCollectionId,
        int? TailSmartCollectionId,
        int? TailMediaItemId,
        string CustomTitle,
        GuideMode GuideMode,
        int? PreRollFillerId,
        int? MidRollFillerId,
        int? PostRollFillerId,
        int? FallbackFillerId) : IRequest<Either<BaseError, ProgramScheduleItemViewModel>>, IProgramScheduleItemRequest;
}
