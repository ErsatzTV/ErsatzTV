using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Emby;

public class EmbyCollectionScanner : IEmbyCollectionScanner
{
    private readonly IEmbyApiClient _embyApiClient;
    private readonly IEmbyCollectionRepository _embyCollectionRepository;
    private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
    private readonly ILogger<EmbyCollectionScanner> _logger;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    public EmbyCollectionScanner(
        IEmbyCollectionRepository embyCollectionRepository,
        IEmbyApiClient embyApiClient,
        ISearchRepository searchRepository,
        ISearchIndex searchIndex,
        IFallbackMetadataProvider fallbackMetadataProvider,
        ILogger<EmbyCollectionScanner> logger)
    {
        _embyCollectionRepository = embyCollectionRepository;
        _embyApiClient = embyApiClient;
        _searchRepository = searchRepository;
        _searchIndex = searchIndex;
        _fallbackMetadataProvider = fallbackMetadataProvider;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanCollections(string address, string apiKey)
    {
        try
        {
            var incomingItemIds = new List<string>();

            // get all collections from db (item id, etag)
            List<EmbyCollection> existingCollections = await _embyCollectionRepository.GetCollections();

            await foreach (EmbyCollection collection in _embyApiClient.GetCollectionLibraryItems(address, apiKey))
            {
                incomingItemIds.Add(collection.ItemId);

                Option<EmbyCollection> maybeExisting = existingCollections.Find(c => c.ItemId == collection.ItemId);

                // skip if unchanged (etag)
                if (await maybeExisting.Map(e => e.Etag ?? string.Empty).IfNoneAsync(string.Empty) ==
                    collection.Etag)
                {
                    _logger.LogDebug("Emby collection {Name} is unchanged", collection.Name);
                    continue;
                }

                // add if new
                if (maybeExisting.IsNone)
                {
                    _logger.LogDebug("Emby collection {Name} is new", collection.Name);
                    await _embyCollectionRepository.AddCollection(collection);
                }
                else
                {
                    _logger.LogDebug("Emby collection {Name} has been updated", collection.Name);
                }

                await SyncCollectionItems(address, apiKey, collection);

                // save collection etag
                await _embyCollectionRepository.SetEtag(collection);
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

    private async Task SyncCollectionItems(
        string address,
        string apiKey,
        EmbyCollection collection)
    {
        try
        {
            // get collection items from Emby
            IAsyncEnumerable<MediaItem> items = _embyApiClient.GetCollectionItems(address, apiKey, collection.ItemId);

            List<int> removedIds = await _embyCollectionRepository.RemoveAllTags(collection);

            // sync tags on items
            var addedIds = new List<int>();
            await foreach (MediaItem item in items)
            {
                addedIds.Add(await _embyCollectionRepository.AddTag(item, collection));
            }

            _logger.LogDebug("Emby collection {Name} contains {Count} items", collection.Name, addedIds.Count);

            var changedIds = removedIds.Except(addedIds).ToList();
            changedIds.AddRange(addedIds.Except(removedIds));

            await _searchIndex.RebuildItems(_searchRepository, _fallbackMetadataProvider, changedIds);
            _searchIndex.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to synchronize Emby collection {Name}", collection.Name);
        }
    }
}
