using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

public record UpdateDeco(
    int DecoId,
    int DecoGroupId,
    string Name,
    DecoMode WatermarkMode,
    List<int> WatermarkIds,
    bool UseWatermarkDuringFiller,
    DecoMode GraphicsElementsMode,
    List<int> GraphicsElementIds,
    bool UseGraphicsElementsDuringFiller,
    DecoMode DefaultFillerMode,
    CollectionType DefaultFillerCollectionType,
    int? DefaultFillerCollectionId,
    int? DefaultFillerMediaItemId,
    int? DefaultFillerMultiCollectionId,
    int? DefaultFillerSmartCollectionId,
    bool DefaultFillerTrimToFit,
    DecoMode DeadAirFallbackMode,
    CollectionType DeadAirFallbackCollectionType,
    int? DeadAirFallbackCollectionId,
    int? DeadAirFallbackMediaItemId,
    int? DeadAirFallbackMultiCollectionId,
    int? DeadAirFallbackSmartCollectionId)
    : IRequest<Either<BaseError, Unit>>;
