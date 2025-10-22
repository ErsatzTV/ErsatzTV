using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Scanner.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Emby;

public class EmbyCollectionScanner : IEmbyCollectionScanner
{
    private readonly IEmbyApiClient _embyApiClient;
    private readonly IScannerProxy _scannerProxy;
    private readonly IEmbyCollectionRepository _embyCollectionRepository;
    private readonly ILogger<EmbyCollectionScanner> _logger;

    public EmbyCollectionScanner(
        IScannerProxy scannerProxy,
        IEmbyCollectionRepository embyCollectionRepository,
        IEmbyApiClient embyApiClient,
        ILogger<EmbyCollectionScanner> logger)
    {
        _scannerProxy = scannerProxy;
        _embyCollectionRepository = embyCollectionRepository;
        _embyApiClient = embyApiClient;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanCollections(string address, string apiKey)
    {
        try
        {
            // need to call get libraries to find library that contains collections (box sets)
            await _embyApiClient.GetLibraries(address, apiKey);

            var incomingItemIds = new List<string>();

            // get all collections from db (item id, etag)
            List<EmbyCollection> existingCollections = await _embyCollectionRepository.GetCollections();

            await foreach ((EmbyCollection collection, int _) in _embyApiClient.GetCollectionLibraryItems(
                               address,
                               apiKey))
            {
                incomingItemIds.Add(collection.ItemId);

                Option<EmbyCollection> maybeExisting = existingCollections.Find(c => c.ItemId == collection.ItemId);

                // // skip if unchanged (etag)
                // if (await maybeExisting.Map(e => e.Etag ?? string.Empty).IfNoneAsync(string.Empty) ==
                //     collection.Etag)
                // {
                //     _logger.LogDebug("Emby collection {Name} is unchanged", collection.Name);
                //     continue;
                // }

                // add if new
                if (maybeExisting.IsNone)
                {
                    _logger.LogDebug("Emby collection {Name} is new", collection.Name);
                    await _embyCollectionRepository.AddCollection(collection);
                }

                if (await SyncCollectionItems(address, apiKey, collection))
                {
                    // save collection etag
                    await _embyCollectionRepository.SetEtag(collection);
                }
            }

            // remove missing collections (and remove any lingering tags from those collections)
            foreach (EmbyCollection collection in existingCollections.Filter(e => !incomingItemIds.Contains(e.ItemId)))
            {
                await _embyCollectionRepository.RemoveCollection(collection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get collections from Emby");
            return BaseError.New(ex.Message);
        }

        return Unit.Default;
    }

    private async Task<bool> SyncCollectionItems(
        string address,
        string apiKey,
        EmbyCollection collection)
    {
        try
        {
            // get collection items from Emby
            IAsyncEnumerable<Tuple<MediaItem, int>> items = _embyApiClient.GetCollectionItems(
                address,
                apiKey,
                collection.ItemId);

            List<int> removedIds = await _embyCollectionRepository.RemoveAllTags(collection);

            // sync tags on items
            var addedIds = new List<int>();
            await foreach ((MediaItem item, int _) in items)
            {
                addedIds.Add(await _embyCollectionRepository.AddTag(item, collection));
            }

            _logger.LogDebug("Emby collection {Name} contains {Count} items", collection.Name, addedIds.Count);

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
            _logger.LogWarning(ex, "Failed to synchronize Emby collection {Name}", collection.Name);
            return false;
        }
    }
}
