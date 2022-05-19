using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddArtistToCollection
    (int CollectionId, int ArtistId) : IRequest<Either<BaseError, Unit>>;
