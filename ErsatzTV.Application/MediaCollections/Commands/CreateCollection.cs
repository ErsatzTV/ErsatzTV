using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record CreateCollection(string Name) : IRequest<Either<BaseError, MediaCollectionViewModel>>;