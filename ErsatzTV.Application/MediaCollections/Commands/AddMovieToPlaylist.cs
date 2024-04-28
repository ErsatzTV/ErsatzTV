using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddMovieToPlaylist(int PlaylistId, int MovieId) : IRequest<Either<BaseError, Unit>>;
