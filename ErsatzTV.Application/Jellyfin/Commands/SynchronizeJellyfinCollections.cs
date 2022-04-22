using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record SynchronizeJellyfinCollections(int JellyfinMediaSourceId) : IRequest<Either<BaseError, Unit>>,
    IJellyfinBackgroundServiceRequest;
