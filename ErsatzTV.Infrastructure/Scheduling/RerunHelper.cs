using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Scheduling;

public class RerunHelper(IDbContextFactory<TvContext> dbContextFactory) : IRerunHelper
{
    private readonly Dictionary<int, List<MediaItem>> _mediaItems = new();
    private readonly Dictionary<int, System.Collections.Generic.HashSet<int>> _history = new();
    private readonly Dictionary<int, PlaybackOrder> _firstRunOrder = new();
    private readonly Dictionary<int, PlaybackOrder> _rerunOrder = new();

    public async Task InitWithMediaItems(
        int playoutId,
        CollectionKey collectionKey,
        List<MediaItem> mediaItems,
        CancellationToken cancellationToken)
    {
        if (_mediaItems.TryAdd(collectionKey.RerunCollectionId!.Value, mediaItems))
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            Option<RerunCollection> maybeRerunCollection = await dbContext.RerunCollections
                .AsNoTracking()
                .SelectOneAsync(
                    rc => rc.Id == collectionKey.RerunCollectionId,
                    rc => rc.Id == collectionKey.RerunCollectionId,
                    cancellationToken);

            foreach (RerunCollection rerunCollection in maybeRerunCollection)
            {
                // load history
                System.Collections.Generic.HashSet<int> history = await dbContext.RerunHistory
                    .AsNoTracking()
                    .Filter(rh => rh.PlayoutId == playoutId && rh.RerunCollectionId == collectionKey.RerunCollectionId)
                    .Map(rh => rh.MediaItemId)
                    .ToHashSetAsync(cancellationToken);
                _history.Add(collectionKey.RerunCollectionId!.Value, history);

                // save playback orders
                _firstRunOrder.Add(collectionKey.RerunCollectionId!.Value, rerunCollection.FirstRunPlaybackOrder);
                _rerunOrder.Add(collectionKey.RerunCollectionId!.Value, rerunCollection.RerunPlaybackOrder);
            }
        }
    }

    public IMediaCollectionEnumerator CreateEnumerator(CollectionKey collectionKey, CollectionEnumeratorState state)
    {
        var playbackOrder = collectionKey.CollectionType is CollectionType.RerunFirstRun
            ? _firstRunOrder[collectionKey.RerunCollectionId!.Value]
            : _rerunOrder[collectionKey.RerunCollectionId!.Value];

        return RerunMediaCollectionEnumerator.Create(
            this,
            collectionKey,
            _mediaItems[collectionKey.RerunCollectionId!.Value],
            playbackOrder,
            state);
    }

    public bool IsFirstRun(CollectionKey collectionKey, int mediaItemId) =>
        !_history[collectionKey.RerunCollectionId!.Value].Contains(mediaItemId);

    public bool IsRerun(CollectionKey collectionKey, int mediaItemId) =>
        _history[collectionKey.RerunCollectionId!.Value].Contains(mediaItemId);

    public int FirstRunCount(CollectionKey collectionKey) =>
        _mediaItems[collectionKey.RerunCollectionId!.Value].Count -
        _history[collectionKey.RerunCollectionId!.Value].Count;

    public int RerunCount(CollectionKey collectionKey) => _history[collectionKey.RerunCollectionId!.Value].Count;

    public void AddToHistory(CollectionKey collectionKey, int mediaItemId) =>
        _history[collectionKey.RerunCollectionId!.Value].Add(mediaItemId);
}
