using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Scheduling;

public record BlockItemViewModel(
    int Id,
    int Index,
    CollectionType CollectionType,
    MediaCollectionViewModel Collection,
    MultiCollectionViewModel MultiCollection,
    SmartCollectionViewModel SmartCollection,
    NamedMediaItemViewModel MediaItem,
    PlaybackOrder PlaybackOrder,
    bool IncludeInProgramGuide,
    bool DisableWatermarks);
