using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Plex;

public interface IPlexCollectionScanner
{
    Task<Either<BaseError, Unit>> ScanCollections(
        PlexConnection connection,
        PlexServerAuthToken token,
        CancellationToken cancellationToken);
}
