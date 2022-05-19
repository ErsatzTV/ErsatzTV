using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Emby;

public class EmbyCollectionScanner : IEmbyCollectionScanner
{
    private readonly IEmbyApiClient _embyApiClient;
    private readonly IEmbyCollectionRepository _embyCollectionRepository;
    private readonly ILogger<EmbyCollectionScanner> _logger;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    public EmbyCollectionScanner(
        IEmbyCollectionRepository embyCollectionRepository,
        IEmbyApiClient embyApiClient,
        ISearchRepository searchRepository,
        ISearchIndex searchIndex,
        ILogger<EmbyCollectionScanner> logger)
    {
        _embyCollectionRepository = embyCollectionRepository;
        _embyApiClient = embyApiClient;
        _searchRepository = searchRepository;
        _searchIndex = searchIndex;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanCollections(string address, string apiKey)
    {
        // get all collections from db (item id, etag)
        List<EmbyCollection> existingCollections = await _embyCollectionRepository.GetCollections();

        // get all collections from emby
        Either<BaseError, List<EmbyCollection>> maybeIncomingCollections =
            await _embyApiClient.GetCollectionLibraryItems(address, apiKey);

        foreach (BaseError error in maybeIncomingCollections.LeftToSeq())
        {
            _logger.LogWarning("Failed to get collections from Emby: {Error}", error.ToString());
            return error;
        }

        foreach (List<EmbyCollection> incomingCollections in maybeIncomingCollections.RightToSeq())
        {
            // loop over collections
            foreach (EmbyCollection collection in incomingCollections)
            {
                Option<EmbyCollection> maybeExisting = existingCollections.Find(c => c.ItemId == collection.ItemId);

                // skip if unchanged (etag)
                if (await maybeExisting.Map(e => e.Etag ?? string.Empty).IfNoneAsync(string.Empty) == collection.Etag)
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
            foreach (EmbyCollection collection in existingCollections
                         .Filter(e => incomingCollections.All(i => i.ItemId != e.ItemId)))
            {
                await _embyCollectionRepository.RemoveCollection(collection);
            }
        }

        return Unit.Default;
    }

    private async Task SyncCollectionItems(
        string address,
        string apiKey,
        EmbyCollection collection)
    {
        // get collection items from JF
        Either<BaseError, List<MediaItem>> maybeItems =
            await _embyApiClient.GetCollectionItems(address, apiKey, collection.ItemId);

        foreach (BaseError error in maybeItems.LeftToSeq())
        {
            _logger.LogWarning("Failed to get collection items from Emby: {Error}", error.ToString());
            return;
        }

        List<int> removedIds = await _embyCollectionRepository.RemoveAllTags(collection);

        var embyItems = maybeItems.RightToSeq().Flatten().ToList();
        _logger.LogDebug("Emby collection {Name} contains {Count} items", collection.Name, embyItems.Count);

        // sync tags on items
        var addedIds = new List<int>();
        foreach (MediaItem item in embyItems)
        {
            addedIds.Add(await _embyCollectionRepository.AddTag(item, collection));
        }

        var changedIds = removedIds.Except(addedIds).ToList();
        changedIds.AddRange(addedIds.Except(removedIds));

        await _searchIndex.RebuildItems(_searchRepository, changedIds);
        _searchIndex.Commit();
    }
}
