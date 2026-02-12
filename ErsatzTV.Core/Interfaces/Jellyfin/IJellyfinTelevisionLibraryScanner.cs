using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinTelevisionLibraryScanner
{
    Task<Either<BaseError, Unit>> ScanLibrary(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library,
        bool deepScan,
        CancellationToken cancellationToken);

    Task<Either<BaseError, Unit>> ScanSingleShow(
        JellyfinConnectionParameters connectionParameters,
        JellyfinLibrary library,
        string showId,
        string showTitle,
        bool deepScan,
        CancellationToken cancellationToken);
}
