using ErsatzTV.Application.Graphics;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
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
    DecoMode BreakContentMode,
    DecoMode DefaultFillerMode,
    CollectionType DefaultFillerCollectionType,
    MediaCollectionViewModel DefaultFillerCollection,
    NamedMediaItemViewModel DefaultFillerMediaItem,
    MultiCollectionViewModel DefaultFillerMultiCollection,
    SmartCollectionViewModel DefaultFillerSmartCollection,
    bool DefaultFillerTrimToFit,
    DecoMode DeadAirFallbackMode,
    CollectionType DeadAirFallbackCollectionType,
    MediaCollectionViewModel DeadAirFallbackCollection,
    NamedMediaItemViewModel DeadAirFallbackMediaItem,
    MultiCollectionViewModel DeadAirFallbackMultiCollection,
    SmartCollectionViewModel DeadAirFallbackSmartCollection);
