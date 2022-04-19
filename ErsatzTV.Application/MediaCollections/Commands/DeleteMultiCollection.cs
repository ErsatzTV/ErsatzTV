using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record DeleteMultiCollection(int MultiCollectionId) : IRequest<Either<BaseError, Unit>>;
