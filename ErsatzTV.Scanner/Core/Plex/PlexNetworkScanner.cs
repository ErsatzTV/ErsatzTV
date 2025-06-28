using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.MediaSources;
using ErsatzTV.Core.Plex;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Plex;

public class PlexNetworkScanner(
    IPlexServerApiClient plexServerApiClient,
    IPlexTelevisionRepository plexTelevisionRepository,
    IMediator mediator,
    ILogger<PlexNetworkScanner> logger) : IPlexNetworkScanner
{
    public async Task<Either<BaseError, Unit>> ScanNetworks(
        PlexLibrary library,
        PlexConnection connection,
        PlexServerAuthToken token,
        CancellationToken cancellationToken)
    {
        // logger.LogDebug("Scanning Plex networks...");

        await foreach ((PlexTag tag, int _) in plexServerApiClient.GetAllTags(
                           connection,
                           token,
                           319,
                           cancellationToken))
        {
            // logger.LogDebug("Found Plex network {Tag}", tag.Tag);

            await SyncNetworkItems(library, connection, token, tag, cancellationToken);
        }

        return Either<BaseError, Unit>.Right(Unit.Default);
    }

    private async Task SyncNetworkItems(
        PlexLibrary library,
        PlexConnection connection,
        PlexServerAuthToken token,
        PlexTag tag,
        CancellationToken cancellationToken)
    {
        try
        {
            // get network items from Plex
            IAsyncEnumerable<Tuple<PlexShow, int>> items = plexServerApiClient.GetTagShowContents(
                library,
                connection,
                token,
                tag);

            // sync tags (networks) on items
            var addedIds = new System.Collections.Generic.HashSet<int>();
            var keepIds = new System.Collections.Generic.HashSet<int>();
            await foreach ((PlexShow item, int _) in items)
            {
                PlexShowAddTagResult result = await plexTelevisionRepository.AddTag(item, tag);

                foreach (int existing in result.Existing)
                {
                    keepIds.Add(existing);
                }

                foreach (int added in result.Added)
                {
                    addedIds.Add(added);
                    keepIds.Add(added);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            List<int> removedIds = await plexTelevisionRepository.RemoveAllTags(library, tag, keepIds);
            int[] changedIds = removedIds.Concat(addedIds).Distinct().ToArray();

            if (changedIds.Length > 0)
            {
                logger.LogDebug("Plex network {Name} contains {Count} changed items", tag.Tag, changedIds.Length);
            }

            await mediator.Publish(
                new ScannerProgressUpdate(0, null, null, changedIds.ToArray(), []),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to synchronize Plex network {Name}", tag.Tag);
        }
    }
}
