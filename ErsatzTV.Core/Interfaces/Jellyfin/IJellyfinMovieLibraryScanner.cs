using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinMovieLibraryScanner
{
    Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        JellyfinLibrary library,
        bool deepScan,
        CancellationToken cancellationToken);
}
