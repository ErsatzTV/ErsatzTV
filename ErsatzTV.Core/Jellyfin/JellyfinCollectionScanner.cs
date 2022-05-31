using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Jellyfin;

public class JellyfinCollectionScanner : IJellyfinCollectionScanner
{
    private readonly IJellyfinApiClient _jellyfinApiClient;
    private readonly IJellyfinCollectionRepository _jellyfinCollectionRepository;
    private readonly ILogger<JellyfinCollectionScanner> _logger;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    public JellyfinCollectionScanner(
        IJellyfinCollectionRepository jellyfinCollectionRepository,
        IJellyfinApiClient jellyfinApiClient,
        ISearchRepository searchRepository,
        ISearchIndex searchIndex,
        ILogger<JellyfinCollectionScanner> logger)
    {
        _jellyfinCollectionRepository = jellyfinCollectionRepository;
        _jellyfinApiClient = jellyfinApiClient;
        _searchRepository = searchRepository;
        _searchIndex = searchIndex;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanCollections(string address, string apiKey, int mediaSourceId)
    {
        try
        {
            var incomingItemIds = new List<string>();

            // get all collections from db (item id, etag)
            List<JellyfinCollection> existingCollections = await _jellyfinCollectionRepository.GetCollections();

            // loop over collections
            await foreach (JellyfinCollection collection in _jellyfinApiClient.GetCollectionLibraryItems(
                               address,
                               apiKey,
                               mediaSourceId))
            {
                incomingItemIds.Add(collection.ItemId);

                Option<JellyfinCollection> maybeExisting = existingCollections.Find(c => c.ItemId == collection.ItemId);

                // skip if unchanged (etag)
                if (await maybeExisting.Map(e => e.Etag ?? string.Empty).IfNoneAsync(string.Empty) == collection.Etag)
                {
                    _logger.LogDebug("Jellyfin collection {Name} is unchanged", collection.Name);
                    continue;
                }

                // add if new
                if (maybeExisting.IsNone)
                {
                    _logger.LogDebug("Jellyfin collection {Name} is new", collection.Name);
                    await _jellyfinCollectionRepository.AddCollection(collection);
                }
                else
                {
                    _logger.LogDebug("Jellyfin collection {Name} has been updated", collection.Name);
                }

                await SyncCollectionItems(address, apiKey, mediaSourceId, collection);

                // save collection etag
                await _jellyfinCollectionRepository.SetEtag(collection);
            }

            // remove missing collections (and remove any lingering tags from those collections)
            foreach (JellyfinCollection collection in existingCollections.Filter(
                         e => !incomingItemIds.Contains(e.ItemId)))
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
        // get collection items from JF
        Either<BaseError, List<MediaItem>> maybeItems =
            await _jellyfinApiClient.GetCollectionItems(address, apiKey, mediaSourceId, collection.ItemId);

        foreach (BaseError error in maybeItems.LeftToSeq())
        {
            _logger.LogWarning("Failed to get collection items from Jellyfin: {Error}", error.ToString());
            return;
        }

        List<int> removedIds = await _jellyfinCollectionRepository.RemoveAllTags(collection);

        var jellyfinItems = maybeItems.RightToSeq().Flatten().ToList();
        _logger.LogDebug("Jellyfin collection {Name} contains {Count} items", collection.Name, jellyfinItems.Count);

        // sync tags on items
        var addedIds = new List<int>();
        foreach (MediaItem item in jellyfinItems)
        {
            addedIds.Add(await _jellyfinCollectionRepository.AddTag(item, collection));
        }

        var changedIds = removedIds.Except(addedIds).ToList();
        changedIds.AddRange(addedIds.Except(removedIds));

        await _searchIndex.RebuildItems(_searchRepository, changedIds);
        _searchIndex.Commit();
    }
}
