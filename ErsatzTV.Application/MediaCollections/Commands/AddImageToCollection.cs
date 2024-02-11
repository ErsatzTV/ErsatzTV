using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddImageToCollection(int CollectionId, int ImageId) : IRequest<Either<BaseError, Unit>>;
