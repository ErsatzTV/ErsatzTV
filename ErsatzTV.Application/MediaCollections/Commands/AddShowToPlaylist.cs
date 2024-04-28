using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddShowToPlaylist(int PlaylistId, int ShowId) : IRequest<Either<BaseError, Unit>>;
