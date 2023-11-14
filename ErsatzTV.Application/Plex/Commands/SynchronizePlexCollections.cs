using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record SynchronizePlexCollections(int PlexMediaSourceId, bool ForceScan) : IRequest<Either<BaseError, Unit>>,
    IScannerBackgroundServiceRequest;
