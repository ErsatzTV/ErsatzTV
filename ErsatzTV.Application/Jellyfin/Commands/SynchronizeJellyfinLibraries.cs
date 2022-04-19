using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record SynchronizeJellyfinLibraries(int JellyfinMediaSourceId) : IRequest<Either<BaseError, Unit>>,
    IJellyfinBackgroundServiceRequest;
