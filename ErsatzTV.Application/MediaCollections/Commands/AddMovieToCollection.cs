using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddMovieToCollection(int CollectionId, int MovieId) : IRequest<Either<BaseError, Unit>>;
