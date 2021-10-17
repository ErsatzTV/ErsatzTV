using System;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Filler.Commands
{
    public record CreateFillerPreset(
        string Name,
        FillerKind FillerKind,
        FillerMode FillerMode,
        TimeSpan? Duration,
        int? Count,
        int? PadToNearestMinute,
        ProgramScheduleItemCollectionType CollectionType,
        int? CollectionId,
        int? MediaItemId,
        int? MultiCollectionId,
        int? SmartCollectionId
    ) : IRequest<Either<BaseError, Unit>>;
}
