using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.MediaSources;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Jellyfin;

public class JellyfinCollectionScanner : IJellyfinCollectionScanner
{
    private readonly IJellyfinApiClient _jellyfinApiClient;
    private readonly IMediator _mediator;
    private readonly IJellyfinCollectionRepository _jellyfinCollectionRepository;
    private readonly ILogger<JellyfinCollectionScanner> _logger;

    public JellyfinCollectionScanner(
        IMediator mediator,
        IJellyfinCollectionRepository jellyfinCollectionRepository,
        IJellyfinApiClient jellyfinApiClient,
        ILogger<JellyfinCollectionScanner> logger)
    {
        _mediator = mediator;
        _jellyfinCollectionRepository = jellyfinCollectionRepository;
        _jellyfinApiClient = jellyfinApiClient;
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
        try
        {
            // get collection items from JF
            IAsyncEnumerable<MediaItem> items = _jellyfinApiClient.GetCollectionItems(
                address,
                apiKey,
                mediaSourceId,
                collection.ItemId);

            List<int> removedIds = await _jellyfinCollectionRepository.RemoveAllTags(collection);

            // sync tags on items
            var addedIds = new List<int>();
            await foreach (MediaItem item in items)
            {
                addedIds.Add(await _jellyfinCollectionRepository.AddTag(item, collection));
            }

            _logger.LogDebug("Jellyfin collection {Name} contains {Count} items", collection.Name, addedIds.Count);

            int[] changedIds = removedIds.Concat(addedIds).Distinct().ToArray();

            await _mediator.Publish(
                new ScannerProgressUpdate(0, null, null, changedIds.ToArray(), Array.Empty<int>()),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to synchronize Jellyfin collection {Name}", collection.Name);
        }
    }
}
