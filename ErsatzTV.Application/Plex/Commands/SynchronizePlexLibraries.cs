using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record SynchronizePlexLibraries(int PlexMediaSourceId) : MediatR.IRequest<Either<BaseError, Unit>>,
    IPlexBackgroundServiceRequest;