using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Scheduling;

public record ReplaceBlockItem(
    int Index,
    CollectionType CollectionType,
    int? CollectionId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? MediaItemId,
    string SearchTitle,
    string SearchQuery,
    PlaybackOrder PlaybackOrder,
    bool IncludeInProgramGuide,
    bool DisableWatermarks,
    List<int> WatermarkIds,
    List<int> GraphicsElementIds);
