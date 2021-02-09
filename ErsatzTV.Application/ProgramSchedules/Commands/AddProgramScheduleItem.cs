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
        int MediaCollectionId,
        int? MultipleCount,
        TimeSpan? PlayoutDuration,
        bool? OfflineTail) : IRequest<Either<BaseError, ProgramScheduleItemViewModel>>, IProgramScheduleItemRequest;
}
