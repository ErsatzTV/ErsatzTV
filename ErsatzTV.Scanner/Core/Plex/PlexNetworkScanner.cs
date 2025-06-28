using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Plex;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Plex;

public class PlexNetworkScanner(IPlexServerApiClient plexServerApiClient, ILogger<PlexNetworkScanner> logger) : IPlexNetworkScanner
{
    public async Task<Either<BaseError, Unit>> ScanNetworks(
        PlexConnection connection,
        PlexServerAuthToken token,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Scanning Plex networks...");

        await foreach ((PlexTag tag, int _) in plexServerApiClient.GetAllTags(
                           connection,
                           token,
                           319,
                           cancellationToken))
        {
            logger.LogInformation("Found Plex network {Tag}", tag.Tag);
        }

        return Either<BaseError, Unit>.Right(Unit.Default);
    }
}
