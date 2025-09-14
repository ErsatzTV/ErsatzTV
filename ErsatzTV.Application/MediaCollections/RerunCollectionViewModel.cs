using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections;

public record RerunCollectionViewModel(
    int Id,
    string Name,
    CollectionType CollectionType,
    MediaCollectionViewModel Collection,
    MultiCollectionViewModel MultiCollection,
    SmartCollectionViewModel SmartCollection,
    NamedMediaItemViewModel MediaItem,
    PlaybackOrder FirstRunPlaybackOrder,
    PlaybackOrder RerunPlaybackOrder);
