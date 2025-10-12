using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record UpdateSmartCollection(int Id, string Name, string Query)
    : IRequest<Either<BaseError, UpdateSmartCollectionResult>>;
