using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record SynchronizePlexShowById(int PlexLibraryId, int ShowId, bool DeepScan) :
    IRequest<Either<BaseError, string>>, IScannerBackgroundServiceRequest;
