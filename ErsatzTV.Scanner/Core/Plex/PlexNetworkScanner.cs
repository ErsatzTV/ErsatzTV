using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using ErsatzTV.Scanner.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Plex;

public class PlexNetworkScanner(
    IPlexServerApiClient plexServerApiClient,
    IPlexTelevisionRepository plexTelevisionRepository,
    ITelevisionRepository televisionRepository,
    IScannerProxy scannerProxy,
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
                PlexShowAddTagResult result = await plexTelevisionRepository.AddTag(
                    library,
                    item,
                    tag,
                    cancellationToken);

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

            List<int> removedIds =
                await plexTelevisionRepository.RemoveAllTags(library, tag, keepIds, cancellationToken);
            var changedIds = removedIds.Concat(addedIds).Distinct().ToList();

            if (changedIds.Count > 0)
            {
                logger.LogDebug("Plex network {Name} contains {Count} changed items", tag.Tag, changedIds.Count);
            }

            foreach (int showId in changedIds.ToArray())
            {
                changedIds.AddRange(await televisionRepository.GetEpisodeIdsForShow(showId));
            }

            if (!await scannerProxy.ReindexMediaItems(changedIds.ToArray(), CancellationToken.None))
            {
                logger.LogWarning("Failed to reindex media items from scanner process");
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            // do nothing
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to synchronize Plex network {Name}", tag.Tag);
        }
    }
}
