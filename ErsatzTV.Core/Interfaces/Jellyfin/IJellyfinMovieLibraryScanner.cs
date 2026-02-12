using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinMovieLibraryScanner
{
    Task<Either<BaseError, Unit>> ScanLibrary(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library,
        bool deepScan,
        CancellationToken cancellationToken);
}
