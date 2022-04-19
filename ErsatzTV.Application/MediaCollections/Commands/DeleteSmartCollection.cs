using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record DeleteSmartCollection(int SmartCollectionId) : IRequest<Either<BaseError, Unit>>;
