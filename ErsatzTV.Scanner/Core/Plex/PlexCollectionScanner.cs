using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using ErsatzTV.Scanner.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Plex;

public class PlexCollectionScanner : IPlexCollectionScanner
{
    private readonly ILogger<PlexCollectionScanner> _logger;
    private readonly IScannerProxy _scannerProxy;
    private readonly IPlexCollectionRepository _plexCollectionRepository;
    private readonly IPlexServerApiClient _plexServerApiClient;

    public PlexCollectionScanner(
        IScannerProxy scannerProxy,
        IPlexCollectionRepository plexCollectionRepository,
        IPlexServerApiClient plexServerApiClient,
        ILogger<PlexCollectionScanner> logger)
    {
        _scannerProxy = scannerProxy;
        _plexCollectionRepository = plexCollectionRepository;
        _plexServerApiClient = plexServerApiClient;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanCollections(
        PlexConnection connection,
        PlexServerAuthToken token,
        CancellationToken cancellationToken)
    {
        try
        {
            var incomingKeys = new List<string>();

            // get all collections from db (key, etag)
            List<PlexCollection> existingCollections = await _plexCollectionRepository.GetCollections();

            await foreach ((PlexCollection collection, int _) in _plexServerApiClient.GetAllCollections(
                               connection,
                               token,
                               cancellationToken))
            {
                incomingKeys.Add(collection.Key);

                Option<PlexCollection> maybeExisting = existingCollections.Find(c => c.Key == collection.Key);

                // skip if unchanged (etag)
                if (await maybeExisting.Map(e => e.Etag ?? string.Empty).IfNoneAsync(string.Empty) ==
                    collection.Etag)
                {
                    _logger.LogDebug("Plex collection {Name} is unchanged", collection.Name);
                    continue;
                }

                // add if new
                if (maybeExisting.IsNone)
                {
                    _logger.LogDebug("Plex collection {Name} is new", collection.Name);
                    await _plexCollectionRepository.AddCollection(collection);
                }

                if (await SyncCollectionItems(connection, token, collection, cancellationToken))
                {
                    // save collection etag
                    await _plexCollectionRepository.SetEtag(collection);
                }
            }

            // remove missing collections (and remove any lingering tags from those collections)
            foreach (PlexCollection collection in existingCollections.Filter(e => !incomingKeys.Contains(e.Key)))
            {
                await _plexCollectionRepository.RemoveCollection(collection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get collections from Plex");
            return BaseError.New(ex.Message);
        }

        return Unit.Default;
    }

    private async Task<bool> SyncCollectionItems(
        PlexConnection connection,
        PlexServerAuthToken token,
        PlexCollection collection,
        CancellationToken cancellationToken)
    {
        try
        {
            // get collection items from Plex
            IAsyncEnumerable<Tuple<MediaItem, int>> items = _plexServerApiClient.GetCollectionItems(
                connection,
                token,
                collection.Key,
                cancellationToken);

            List<int> removedIds = await _plexCollectionRepository.RemoveAllTags(collection);

            // sync tags on items
            var addedIds = new List<int>();
            await foreach ((MediaItem item, int _) in items)
            {
                addedIds.Add(await _plexCollectionRepository.AddTag(item, collection));
                cancellationToken.ThrowIfCancellationRequested();
            }

            _logger.LogDebug("Plex collection {Name} contains {Count} items", collection.Name, addedIds.Count);

            int[] changedIds = removedIds.Concat(addedIds).Distinct().ToArray();
            if (!await _scannerProxy.ReindexMediaItems(changedIds, CancellationToken.None))
            {
                _logger.LogWarning("Failed to reindex media items from scanner process");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to synchronize Plex collection {Name}", collection.Name);
            return false;
        }
    }
}
