using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

public record DecoBreakContentViewModel(
    int Id,
    CollectionType CollectionType,
    MediaCollectionViewModel Collection,
    NamedMediaItemViewModel MediaItem,
    MultiCollectionViewModel MultiCollection,
    SmartCollectionViewModel SmartCollection,
    PlaylistViewModel Playlist,
    DecoBreakPlacement Placement);
