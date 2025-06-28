using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Plex;

public interface IPlexNetworkScanner
{
    Task<Either<BaseError, Unit>> ScanNetworks(
        PlexLibrary library,
        PlexConnection connection,
        PlexServerAuthToken token,
        CancellationToken cancellationToken);
}
