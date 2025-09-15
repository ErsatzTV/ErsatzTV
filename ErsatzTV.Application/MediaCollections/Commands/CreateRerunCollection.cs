using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections;

public record CreateRerunCollection(
    string Name,
    CollectionType CollectionType,
    MediaCollectionViewModel Collection,
    MultiCollectionViewModel MultiCollection,
    SmartCollectionViewModel SmartCollection,
    NamedMediaItemViewModel MediaItem,
    PlaybackOrder FirstRunPlaybackOrder,
    PlaybackOrder RerunPlaybackOrder)
    : IRequest<Either<BaseError, RerunCollectionViewModel>>;
