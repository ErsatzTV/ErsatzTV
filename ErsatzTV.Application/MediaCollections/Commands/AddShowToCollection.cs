using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddShowToCollection(int CollectionId, int ShowId) : IRequest<Either<BaseError, Unit>>;
