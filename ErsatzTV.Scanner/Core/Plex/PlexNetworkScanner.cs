using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Plex;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Plex;

public class PlexNetworkScanner(ILogger<PlexNetworkScanner> logger) : IPlexNetworkScanner
{
    public Task<Either<BaseError, Unit>> ScanNetworks(
        PlexConnection connectionParametersActiveConnection,
        PlexServerAuthToken connectionParametersPlexServerAuthToken,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Scanning Plex networks...");
        return Task.FromResult(Either<BaseError, Unit>.Right(Unit.Default));
    }
}
