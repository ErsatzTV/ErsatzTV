using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddMediaItemToCollection(int CollectionId, int MediaItemId) : IRequest<Either<BaseError, Unit>>;
