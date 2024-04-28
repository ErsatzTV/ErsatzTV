using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddEpisodeToPlaylist(int PlaylistId, int EpisodeId) : IRequest<Either<BaseError, Unit>>;
