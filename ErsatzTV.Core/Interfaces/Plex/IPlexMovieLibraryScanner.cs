using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Plex;

public interface IPlexMovieLibraryScanner
{
    Task<Either<BaseError, Unit>> ScanLibrary(
        PlexConnection connection,
        PlexServerAuthToken token,
        PlexLibrary library,
        bool deepScan,
        CancellationToken cancellationToken);
}
