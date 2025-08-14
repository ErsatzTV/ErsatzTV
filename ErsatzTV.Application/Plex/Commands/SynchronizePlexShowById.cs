using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record SynchronizePlexShowById(int PlexLibraryId, int ShowId, string ShowTitle, bool DeepScan) :
    IRequest<Either<BaseError, string>>, IScannerBackgroundServiceRequest;
