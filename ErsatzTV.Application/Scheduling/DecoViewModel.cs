using ErsatzTV.Application.Graphics;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

public record DecoViewModel(
    int Id,
    int DecoGroupId,
    string DecoGroupName,
    string Name,
    DecoMode WatermarkMode,
    List<WatermarkViewModel> Watermarks,
    bool UseWatermarkDuringFiller,
    DecoMode GraphicsElementsMode,
    List<GraphicsElementViewModel> GraphicsElements,
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
    int? DeadAirFallbackSmartCollectionId);
