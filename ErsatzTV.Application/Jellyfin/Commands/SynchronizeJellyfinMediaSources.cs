using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Jellyfin;

public record SynchronizeJellyfinMediaSources : IRequest<Either<BaseError, List<JellyfinMediaSource>>>,
    IJellyfinBackgroundServiceRequest;
