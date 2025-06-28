using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Plex;

public interface IPlexNetworkScanner
{
    Task<Either<BaseError, Unit>> ScanNetworks(
        PlexConnection connectionParametersActiveConnection,
        PlexServerAuthToken connectionParametersPlexServerAuthToken,
        CancellationToken cancellationToken);
}
