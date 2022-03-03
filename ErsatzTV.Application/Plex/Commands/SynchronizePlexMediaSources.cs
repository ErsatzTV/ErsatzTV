using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Plex;

public record
    SynchronizePlexMediaSources : IRequest<Either<BaseError, List<PlexMediaSource>>>, IPlexBackgroundServiceRequest;