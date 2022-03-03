using System;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;

namespace ErsatzTV.Application.Filler;

public record FillerPresetViewModel(
    int Id,
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
    int? SmartCollectionId);