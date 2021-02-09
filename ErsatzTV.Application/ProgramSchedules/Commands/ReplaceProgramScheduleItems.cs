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
        int MediaCollectionId,
        int? MultipleCount,
        TimeSpan? PlayoutDuration,
        bool? OfflineTail) : IProgramScheduleItemRequest;

    public record ReplaceProgramScheduleItems
        (int ProgramScheduleId, List<ReplaceProgramScheduleItem> Items) : IRequest<
            Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>>;
}
