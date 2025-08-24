using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.BlockScheduling;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.Engine;

public class SchedulingEngine(IMediaCollectionRepository mediaCollectionRepository, ILogger<SchedulingEngine> logger)
    : ISchedulingEngine
{
    private readonly Dictionary<string, IMediaCollectionEnumerator> _enumerators = new();
    private readonly SchedulingEngineState _state = new();
    private PlayoutReferenceData _referenceData;

    public ISchedulingEngine WithMode(PlayoutBuildMode mode)
    {
        _state.Mode = mode;
        return this;
    }

    public ISchedulingEngine WithSeed(int seed)
    {
        _state.Seed = seed;
        return this;
    }

    public ISchedulingEngine WithReferenceData(PlayoutReferenceData referenceData)
    {
        _referenceData = referenceData;
        return this;
    }

    public ISchedulingEngine BuildBetween(DateTimeOffset start, DateTimeOffset finish)
    {
        _state.Start = start;
        _state.Finish = finish;
        _state.CurrentTime = start;
        return this;
    }

    public ISchedulingEngine RemoveBefore(DateTimeOffset removeBefore)
    {
        _state.RemoveBefore = removeBefore;
        return this;
    }

    public ISchedulingEngine RestoreOrReset(Option<PlayoutAnchor> maybeAnchor)
    {
        if (_state.Mode is PlayoutBuildMode.Reset)
        {
            // erase items, not history
            _state.ClearItems = true;

            // remove any future or "currently active" history items
            // this prevents "walking" the playout forward by repeatedly resetting
            var toRemove = new List<PlayoutHistory>();
            toRemove.AddRange(
                _referenceData.PlayoutHistory.Filter(h =>
                    h.When > _state.Start.UtcDateTime ||
                    h.When <= _state.Start.UtcDateTime && h.Finish >= _state.Start.UtcDateTime));
            foreach (PlayoutHistory history in toRemove)
            {
                _state.HistoryToRemove.Add(history.Id);
            }
        }
        else
        {
            foreach (PlayoutAnchor anchor in maybeAnchor)
            {
                _state.CurrentTime = new DateTimeOffset(anchor.NextStart.ToLocalTime(), _state.CurrentTime.Offset);

                if (string.IsNullOrWhiteSpace(anchor.Context))
                {
                    break;
                }

                // TODO: load the rest of the context
            }
        }

        return this;
    }

    public async Task<ISchedulingEngine> AddCollection(string key, string collectionName, PlaybackOrder playbackOrder)
    {
        if (!_enumerators.ContainsKey(key))
        {
            int index = _enumerators.Count;
            List<MediaItem> items = await mediaCollectionRepository.GetCollectionItemsByName(collectionName);
            if (items.Count == 0)
            {
                logger.LogWarning("Skipping invalid or empty collection {Name}", collectionName);
                return this;
            }

            var state = new CollectionEnumeratorState { Seed = _state.Seed + index, Index = 0 };
            foreach (var enumerator in EnumeratorForContent(items, state, playbackOrder))
            {
                if (_enumerators.TryAdd(key, enumerator))
                {
                    logger.LogDebug(
                        "Added collection {Name} with key {Key} and order {Order}",
                        collectionName,
                        key,
                        playbackOrder);

                    string historyKey = HistoryDetails.KeyForSchedulingContent(key, playbackOrder);
                    ApplyHistory(historyKey, items, enumerator, playbackOrder);
                }
            }
        }

        return this;
    }

    public ISchedulingEngineState GetState()
    {
        return _state;
    }

    private static Option<IMediaCollectionEnumerator> EnumeratorForContent(
        List<MediaItem> items,
        CollectionEnumeratorState state,
        PlaybackOrder playbackOrder,
        bool multiPart = false)
    {
        switch (playbackOrder)
        {
            case PlaybackOrder.Chronological:
                return new ChronologicalMediaCollectionEnumerator(items, state);
            case PlaybackOrder.Shuffle:
                bool keepMultiPartEpisodesTogether = multiPart;
                List<GroupedMediaItem> groupedMediaItems = keepMultiPartEpisodesTogether
                    ? MultiPartEpisodeGrouper.GroupMediaItems(items, false)
                    : items.Map(mi => new GroupedMediaItem(mi, null)).ToList();
                return new BlockPlayoutShuffledMediaCollectionEnumerator(groupedMediaItems, state);
        }

        return Option<IMediaCollectionEnumerator>.None;
    }

    private void ApplyHistory(
        string historyKey,
        List<MediaItem> collectionItems,
        IMediaCollectionEnumerator enumerator,
        PlaybackOrder playbackOrder)
    {
        DateTime historyTime = _state.CurrentTime.UtcDateTime;

        var filteredHistory = _referenceData.PlayoutHistory.ToList();
        filteredHistory.RemoveAll(h => _state.HistoryToRemove.Contains(h.Id));

        Option<DateTime> maxWhen = filteredHistory
            .Filter(h => h.Key == historyKey)
            .Filter(h => h.When < historyTime)
            .Map(h => h.When)
            .OrderByDescending(h => h)
            .HeadOrNone()
            .IfNone(DateTime.MinValue);

        var maybeHistory = filteredHistory
            .Filter(h => h.Key == historyKey)
            .Filter(h => h.When == maxWhen)
            .ToList();

        if (enumerator is PlaylistEnumerator playlistEnumerator)
        {
            Option<PlayoutHistory> maybePrimaryHistory = maybeHistory
                .Filter(h => string.IsNullOrWhiteSpace(h.ChildKey))
                .HeadOrNone();

            foreach (PlayoutHistory primaryHistory in maybePrimaryHistory)
            {
                var hasSetEnumeratorIndex = false;

                var childEnumeratorKeys = playlistEnumerator.ChildEnumerators.Map(x => x.CollectionKey).ToList();
                foreach ((IMediaCollectionEnumerator childEnumerator, CollectionKey collectionKey) in
                         playlistEnumerator.ChildEnumerators)
                {
                    PlaybackOrder itemPlaybackOrder = childEnumerator switch
                    {
                        ChronologicalMediaCollectionEnumerator => PlaybackOrder.Chronological,
                        RandomizedMediaCollectionEnumerator => PlaybackOrder.Random,
                        ShuffledMediaCollectionEnumerator => PlaybackOrder.Shuffle,
                        _ => PlaybackOrder.None
                    };

                    Option<PlayoutHistory> maybeApplicableHistory = maybeHistory
                        .Filter(h => h.ChildKey == HistoryDetails.KeyForCollectionKey(collectionKey))
                        .HeadOrNone();

                    if (collectionItems.Count == 0)
                    {
                        continue;
                    }

                    foreach (PlayoutHistory h in maybeApplicableHistory)
                    {
                        // logger.LogDebug(
                        //     "History is applicable: {When}: {ChildKey} / {History} / {IsCurrentChild}",
                        //     h.When,
                        //     h.ChildKey,
                        //     h.Details,
                        //     h.IsCurrentChild);

                        enumerator.ResetState(
                            new CollectionEnumeratorState
                            {
                                Seed = enumerator.State.Seed,
                                Index = h.Index + (h.IsCurrentChild ? 1 : 0)
                            });

                        if (itemPlaybackOrder is PlaybackOrder.Chronological)
                        {
                            HistoryDetails.MoveToNextItem(
                                collectionItems,
                                h.Details,
                                childEnumerator,
                                itemPlaybackOrder,
                                true);
                        }

                        if (h.IsCurrentChild)
                        {
                            // try to find enumerator based on collection key
                            playlistEnumerator.SetEnumeratorIndex(childEnumeratorKeys.IndexOf(collectionKey));
                            hasSetEnumeratorIndex = true;
                        }
                    }
                }

                if (!hasSetEnumeratorIndex)
                {
                    // falling back to enumerator based on index
                    playlistEnumerator.SetEnumeratorIndex(primaryHistory.Index);
                }

                // only move next at the end, because that may also move
                // the enumerator index
                playlistEnumerator.MoveNext();
            }
        }
        else
        {
            if (collectionItems.Count == 0)
            {
                return;
            }

            // seek to the appropriate place in the collection enumerator
            foreach (PlayoutHistory h in maybeHistory)
            {
                // logger.LogDebug("History is applicable: {When}: {History}", h.When, h.Details);

                enumerator.ResetState(
                    new CollectionEnumeratorState { Seed = enumerator.State.Seed, Index = h.Index + 1 });

                if (playbackOrder is PlaybackOrder.Chronological)
                {
                    HistoryDetails.MoveToNextItem(
                        collectionItems,
                        h.Details,
                        enumerator,
                        playbackOrder);
                }
            }
        }
    }

    private class SchedulingEngineState : ISchedulingEngineState
    {
        // state
        public PlayoutBuildMode Mode { get; set; }
        public int Seed { get; set; }
        public DateTimeOffset Finish { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset CurrentTime { get; set; }

        // result
        public DateTimeOffset RemoveBefore { get; set; }
        public bool ClearItems { get; set; }
        public System.Collections.Generic.HashSet<int> HistoryToRemove { get; set; } = [];
    }
}
