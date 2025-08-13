using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Plex;

public interface IPlexTelevisionLibraryScanner
{
    Task<Either<BaseError, Unit>> ScanLibrary(
        PlexConnection connection,
        PlexServerAuthToken token,
        PlexLibrary library,
        bool deepScan,
        CancellationToken cancellationToken);

    Task<Either<BaseError, Unit>> ScanSingleShow(
        PlexConnection connection,
        PlexServerAuthToken token,
        PlexLibrary library,
        string showTitle,
        bool deepScan,
        CancellationToken cancellationToken);
}
