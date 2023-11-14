using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record SynchronizePlexLibraries(int PlexMediaSourceId) : IRequest<Either<BaseError, Unit>>,
    IScannerBackgroundServiceRequest;
