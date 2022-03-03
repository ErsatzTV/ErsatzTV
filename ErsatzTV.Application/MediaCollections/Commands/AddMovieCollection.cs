using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddMovieToCollection(int CollectionId, int MovieId) : MediatR.IRequest<Either<BaseError, Unit>>;