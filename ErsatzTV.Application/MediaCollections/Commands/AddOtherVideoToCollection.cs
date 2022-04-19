using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddOtherVideoToCollection
    (int CollectionId, int OtherVideoId) : IRequest<Either<BaseError, Unit>>;
