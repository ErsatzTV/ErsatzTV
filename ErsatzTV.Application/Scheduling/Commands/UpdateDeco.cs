using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

public record UpdateDecoBreakContent(
    int Id,
    CollectionType CollectionType,
    int? CollectionId,
    int? MediaItemId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? PlaylistId,
    DecoBreakPlacement Placement);

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
    DecoMode BreakContentMode,
    List<UpdateDecoBreakContent> BreakContent,
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
