using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Scheduling;

public class RerunHelper(IDbContextFactory<TvContext> dbContextFactory) : IRerunHelper
{
    private int _playoutId;
    private readonly Dictionary<int, List<MediaItem>> _mediaItems = new();
    private readonly Dictionary<int, System.Collections.Generic.HashSet<int>> _history = new();
    private readonly Dictionary<int, PlaybackOrder> _firstRunOrder = new();
    private readonly Dictionary<int, PlaybackOrder> _rerunOrder = new();
    private readonly Dictionary<int, List<int>> _historyToRemove = new();
    private readonly Dictionary<int, List<RerunHistory>> _historyToAdd = new();

    public bool ClearHistory { get; set; }

    public async Task InitWithMediaItems(
        int playoutId,
        CollectionKey collectionKey,
        List<MediaItem> mediaItems,
        CancellationToken cancellationToken)
    {
        if (_mediaItems.TryAdd(collectionKey.RerunCollectionId!.Value, mediaItems))
        {
            _playoutId = playoutId;

            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            Option<RerunCollection> maybeRerunCollection = await dbContext.RerunCollections
                .AsNoTracking()
                .SelectOneAsync(
                    rc => rc.Id == collectionKey.RerunCollectionId,
                    rc => rc.Id == collectionKey.RerunCollectionId,
                    cancellationToken);

            foreach (RerunCollection rerunCollection in maybeRerunCollection)
            {
                System.Collections.Generic.HashSet<int> history = await dbContext.RerunHistory
                    .AsNoTracking()
                    .Filter(rh => rh.PlayoutId == playoutId && rh.RerunCollectionId == rerunCollection.Id)
                    .Map(rh => rh.MediaItemId)
                    .ToHashSetAsync(cancellationToken);

                if (ClearHistory)
                {
                    List<int> historyToRemove = await dbContext.RerunHistory
                        .AsNoTracking()
                        .Filter(rh => rh.PlayoutId == playoutId && rh.RerunCollectionId == rerunCollection.Id)
                        .Map(rh => rh.Id)
                        .ToListAsync(cancellationToken);
                    _historyToRemove.Add(rerunCollection.Id, historyToRemove);

                    history.Clear();
                }

                _history.Add(rerunCollection.Id, history);

                // save playback orders
                _firstRunOrder.Add(rerunCollection.Id, rerunCollection.FirstRunPlaybackOrder);
                _rerunOrder.Add(rerunCollection.Id, rerunCollection.RerunPlaybackOrder);

                _historyToAdd.Add(rerunCollection.Id, []);
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

    public void AddToHistory(CollectionKey collectionKey, int mediaItemId, DateTimeOffset scheduledAt)
    {
        var history = new RerunHistory
        {
            PlayoutId = _playoutId,
            RerunCollectionId = collectionKey.RerunCollectionId!.Value,
            MediaItemId = mediaItemId,
            When = scheduledAt.UtcDateTime
        };

        _history[collectionKey.RerunCollectionId!.Value].Add(mediaItemId);
        _historyToAdd[collectionKey.RerunCollectionId!.Value].Add(history);
    }

    public System.Collections.Generic.HashSet<int> GetHistoryToRemove() =>
        _historyToRemove.SelectMany(kvp => kvp.Value).ToHashSet();

    public List<RerunHistory> GetHistoryToAdd() => _historyToAdd.SelectMany(kvp => kvp.Value).ToList();
}
