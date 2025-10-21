using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.MediaSources;
using ErsatzTV.Scanner.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Jellyfin;

public class JellyfinCollectionScanner : IJellyfinCollectionScanner
{
    private readonly IJellyfinApiClient _jellyfinApiClient;
    private readonly IScannerProxy _scannerProxy;
    private readonly IJellyfinCollectionRepository _jellyfinCollectionRepository;
    private readonly ILogger<JellyfinCollectionScanner> _logger;

    public JellyfinCollectionScanner(
        IScannerProxy scannerProxy,
        IJellyfinCollectionRepository jellyfinCollectionRepository,
        IJellyfinApiClient jellyfinApiClient,
        ILogger<JellyfinCollectionScanner> logger)
    {
        _scannerProxy = scannerProxy;
        _jellyfinCollectionRepository = jellyfinCollectionRepository;
        _jellyfinApiClient = jellyfinApiClient;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanCollections(string address, string apiKey, int mediaSourceId)
    {
        try
        {
            // need to call get libraries to find library that contains collections (box sets)
            await _jellyfinApiClient.GetLibraries(address, apiKey);

            var incomingItemIds = new List<string>();

            // get all collections from db (item id, etag)
            List<JellyfinCollection> existingCollections = await _jellyfinCollectionRepository.GetCollections();

            // loop over collections
            await foreach ((JellyfinCollection collection, int _) in _jellyfinApiClient.GetCollectionLibraryItems(
                               address,
                               apiKey,
                               mediaSourceId))
            {
                incomingItemIds.Add(collection.ItemId);

                Option<JellyfinCollection> maybeExisting = existingCollections.Find(c => c.ItemId == collection.ItemId);

                // // skip if unchanged (etag)
                // if (await maybeExisting.Map(e => e.Etag ?? string.Empty).IfNoneAsync(string.Empty) == collection.Etag)
                // {
                //     _logger.LogDebug("Jellyfin collection {Name} is unchanged", collection.Name);
                //     continue;
                // }

                // add if new
                if (maybeExisting.IsNone)
                {
                    _logger.LogDebug("Jellyfin collection {Name} is new", collection.Name);
                    await _jellyfinCollectionRepository.AddCollection(collection);
                }

                await SyncCollectionItems(address, apiKey, mediaSourceId, collection);

                // save collection etag
                await _jellyfinCollectionRepository.SetEtag(collection);
            }

            // remove missing collections (and remove any lingering tags from those collections)
            foreach (JellyfinCollection collection in existingCollections.Filter(e =>
                         !incomingItemIds.Contains(e.ItemId)))
            {
                await _jellyfinCollectionRepository.RemoveCollection(collection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get collections from Jellyfin");
            return BaseError.New(ex.Message);
        }

        return Unit.Default;
    }

    private async Task SyncCollectionItems(
        string address,
        string apiKey,
        int mediaSourceId,
        JellyfinCollection collection)
    {
        try
        {
            // get collection items from JF
            IAsyncEnumerable<Tuple<MediaItem, int>> items = _jellyfinApiClient.GetCollectionItems(
                address,
                apiKey,
                mediaSourceId,
                collection.ItemId);

            List<int> removedIds = await _jellyfinCollectionRepository.RemoveAllTags(collection);

            // sync tags on items
            var addedIds = new List<int>();
            await foreach ((MediaItem item, int _) in items)
            {
                addedIds.Add(await _jellyfinCollectionRepository.AddTag(item, collection));
            }

            _logger.LogDebug("Jellyfin collection {Name} contains {Count} items", collection.Name, addedIds.Count);

            int[] changedIds = removedIds.Concat(addedIds).Distinct().ToArray();
            if (!await _scannerProxy.ReindexMediaItems(changedIds, CancellationToken.None))
            {
                _logger.LogWarning("Failed to reindex media items from scanner process");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to synchronize Jellyfin collection {Name}", collection.Name);
        }
    }
}
