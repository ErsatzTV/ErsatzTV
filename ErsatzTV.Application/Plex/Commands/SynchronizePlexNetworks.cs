using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record SynchronizePlexNetworks(int PlexLibraryId, bool ForceScan) : IRequest<Either<BaseError, Unit>>,
    IScannerBackgroundServiceRequest;
