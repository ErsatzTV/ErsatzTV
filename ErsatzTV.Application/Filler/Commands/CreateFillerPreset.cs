﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;

namespace ErsatzTV.Application.Filler;

public record CreateFillerPreset(
    string Name,
    FillerKind FillerKind,
    FillerMode FillerMode,
    TimeSpan? Duration,
    int? Count,
    int? PadToNearestMinute,
    bool AllowWatermarks,
    ProgramScheduleItemCollectionType CollectionType,
    int? CollectionId,
    int? MediaItemId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? PlaylistId,
    string Expression
) : IRequest<Either<BaseError, Unit>>;
