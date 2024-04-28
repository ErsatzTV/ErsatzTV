using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddSeasonToPlaylist(int PlaylistId, int SeasonId) : IRequest<Either<BaseError, Unit>>;
