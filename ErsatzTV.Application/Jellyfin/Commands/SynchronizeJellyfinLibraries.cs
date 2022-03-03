using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record SynchronizeJellyfinLibraries(int JellyfinMediaSourceId) : MediatR.IRequest<Either<BaseError, Unit>>,
    IJellyfinBackgroundServiceRequest;