using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Plex;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Plex;

public class PlexCollectionScanner : IPlexCollectionScanner
{
    private readonly ILogger<PlexCollectionScanner> _logger;

    public PlexCollectionScanner(ILogger<PlexCollectionScanner> logger)
    {
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanCollections(
        PlexConnection connection,
        PlexServerAuthToken token,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        _logger.LogDebug("Scanning plex collections...");
        
        return Unit.Default;
    }
}
