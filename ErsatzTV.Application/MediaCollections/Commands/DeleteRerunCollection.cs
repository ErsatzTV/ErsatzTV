using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record DeleteRerunCollection(int RerunCollectionId) : IRequest<Either<BaseError, Unit>>;
