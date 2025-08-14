using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinTelevisionLibraryScanner
{
    Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        JellyfinLibrary library,
        bool deepScan,
        CancellationToken cancellationToken);

    Task<Either<BaseError, Unit>> ScanSingleShow(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string showId,
        string showTitle,
        bool deepScan,
        CancellationToken cancellationToken);
}
