using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

public record DecoViewModel(
    int Id,
    int DecoGroupId,
    string Name,
    DecoMode WatermarkMode,
    int? WatermarkId,
    bool UseWatermarkDuringFiller,
    DecoMode DefaultFillerMode,
    ProgramScheduleItemCollectionType DefaultFillerCollectionType,
    int? DefaultFillerCollectionId,
    int? DefaultFillerMediaItemId,
    int? DefaultFillerMultiCollectionId,
    int? DefaultFillerSmartCollectionId,
    bool DefaultFillerTrimToFit,
    DecoMode DeadAirFallbackMode,
    ProgramScheduleItemCollectionType DeadAirFallbackCollectionType,
    int? DeadAirFallbackCollectionId,
    int? DeadAirFallbackMediaItemId,
    int? DeadAirFallbackMultiCollectionId,
    int? DeadAirFallbackSmartCollectionId);
