using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record SynchronizeJellyfinShowById(int JellyfinLibraryId, int ShowId, bool DeepScan)
    : IRequest<Either<BaseError, string>>, IScannerBackgroundServiceRequest;
