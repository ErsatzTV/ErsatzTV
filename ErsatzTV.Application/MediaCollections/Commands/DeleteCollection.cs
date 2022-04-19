using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record DeleteCollection(int CollectionId) : IRequest<Either<BaseError, Unit>>;
