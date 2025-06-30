using System.Collections.Immutable;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public class PlaylistEnumerator : IMediaCollectionEnumerator
{
    private readonly System.Collections.Generic.HashSet<int> _remainingMediaItemIds = [];
    private System.Collections.Generic.HashSet<int> _allMediaItemIds;
    private int _enumeratorIndex;
    private System.Collections.Generic.HashSet<int> _idsToIncludeInEPG;
    private IList<bool> _playAll;
    private List<IMediaCollectionEnumerator> _sortedEnumerators;
    private bool _shufflePlaylistItems;
    private CloneableRandom _random;

    private PlaylistEnumerator()
    {
    }

    public void ResetState(CollectionEnumeratorState state) =>
        // seed doesn't matter here
        State.Index = state.Index;

    public CollectionEnumeratorState State { get; private set; }

    public Option<MediaItem> Current => _sortedEnumerators.Count > 0
        ? _sortedEnumerators[_enumeratorIndex].Current
        : Option<MediaItem>.None;

    public Option<bool> CurrentIncludeInProgramGuide
    {
        get
        {
            foreach (MediaItem mediaItem in Current)
            {
                return _idsToIncludeInEPG.Contains(mediaItem.Id);
            }

            return Option<bool>.None;
        }
    }

    public int Count => throw new NotSupportedException("Count isn't used for playlist enumeration");

    public Option<TimeSpan> MinimumDuration { get; private set; }

    public void MoveNext()
    {
        foreach (MediaItem maybeMediaItem in _sortedEnumerators[_enumeratorIndex].Current)
        {
            _remainingMediaItemIds.Remove(maybeMediaItem.Id);
        }

        _sortedEnumerators[_enumeratorIndex].MoveNext();

        // if we aren't playing all, or if we just finished playing all, move to the next enumerator
        if (!_playAll[_enumeratorIndex] || _sortedEnumerators[_enumeratorIndex].State.Index == 0)
        {
            _enumeratorIndex = (_enumeratorIndex + 1) % _sortedEnumerators.Count;
        }

        State.Index += 1;
        if (_remainingMediaItemIds.Count == 0 && _enumeratorIndex == 0 && _sortedEnumerators[0].State.Index == 0)
        {
            State.Index = 0;
            _remainingMediaItemIds.UnionWith(_allMediaItemIds);

            if (_shufflePlaylistItems)
            {
                State.Seed = _random.Next();
                _random = new CloneableRandom(State.Seed);
                _sortedEnumerators = ShufflePlaylistItems();
            }
        }
    }

    public ImmutableDictionary<CollectionKey, IMediaCollectionEnumerator> ChildEnumerators { get; private set; }

    public int EnumeratorIndex => _enumeratorIndex;

    public void SetEnumeratorIndex(int primaryHistoryIndex)
    {
        _enumeratorIndex = primaryHistoryIndex;
    }

    public static async Task<PlaylistEnumerator> Create(
        IMediaCollectionRepository mediaCollectionRepository,
        Dictionary<PlaylistItem, List<MediaItem>> playlistItemMap,
        CollectionEnumeratorState state,
        bool shufflePlaylistItems,
        CancellationToken cancellationToken)
    {
        var result = new PlaylistEnumerator
        {
            _sortedEnumerators = [],
            _playAll = [],
            _idsToIncludeInEPG = [],
            _shufflePlaylistItems = shufflePlaylistItems
        };

        // collections should share enumerators
        var enumeratorMap = new Dictionary<CollectionKey, IMediaCollectionEnumerator>();
        result._allMediaItemIds = [];

        foreach (PlaylistItem playlistItem in playlistItemMap.Keys.OrderBy(i => i.Index))
        {
            List<MediaItem> items = playlistItemMap[playlistItem];
            foreach (MediaItem mediaItem in items)
            {
                result._allMediaItemIds.Add(mediaItem.Id);

                if (playlistItem.IncludeInProgramGuide)
                {
                    result._idsToIncludeInEPG.Add(mediaItem.Id);
                }
            }

            var collectionKey = CollectionKey.ForPlaylistItem(playlistItem);
            if (enumeratorMap.TryGetValue(collectionKey, out IMediaCollectionEnumerator enumerator))
            {
                result._sortedEnumerators.Add(enumerator);
                result._playAll.Add(playlistItem.PlayAll);
                continue;
            }

            var initState = new CollectionEnumeratorState { Seed = state.Seed, Index = 0 };
            if (items.Count == 1)
            {
                enumerator = new SingleMediaItemEnumerator(items.Head());
            }
            else
            {
                switch (playlistItem.PlaybackOrder)
                {
                    case PlaybackOrder.Chronological:
                        enumerator = new ChronologicalMediaCollectionEnumerator(items, initState);
                        break;
                    // TODO: fix multi episode shuffle?
                    case PlaybackOrder.MultiEpisodeShuffle:
                    case PlaybackOrder.Shuffle:
                        List<GroupedMediaItem> i = await PlayoutBuilder.GetGroupedMediaItemsForShuffle(
                            mediaCollectionRepository,
                            // TODO: fix this
                            new ProgramSchedule { KeepMultiPartEpisodesTogether = false },
                            items,
                            CollectionKey.ForPlaylistItem(playlistItem));
                        enumerator = new ShuffledMediaCollectionEnumerator(i, initState, cancellationToken);
                        break;
                    case PlaybackOrder.ShuffleInOrder:
                        enumerator = new ShuffleInOrderCollectionEnumerator(
                            await PlayoutBuilder.GetCollectionItemsForShuffleInOrder(
                                mediaCollectionRepository,
                                CollectionKey.ForPlaylistItem(playlistItem)),
                            initState,
                            // TODO: fix this
                            false,
                            cancellationToken);
                        break;
                    case PlaybackOrder.SeasonEpisode:
                        // TODO: check random start point?
                        enumerator = new SeasonEpisodeMediaCollectionEnumerator(items, initState);
                        break;
                    case PlaybackOrder.Random:
                        enumerator = new RandomizedMediaCollectionEnumerator(items, initState);
                        break;
                }
            }

            if (enumerator is not null)
            {
                enumeratorMap.Add(collectionKey, enumerator);
                result._sortedEnumerators.Add(enumerator);
                result._playAll.Add(playlistItem.PlayAll);
            }
        }

        result._remainingMediaItemIds.UnionWith(result._allMediaItemIds);

        result.MinimumDuration = playlistItemMap.Values
            .Flatten()
            .Bind(i => i.GetNonZeroDuration())
            .OrderBy(identity)
            .HeadOrNone();

        result._random = new CloneableRandom(state.Seed);

        if (shufflePlaylistItems)
        {
            result._sortedEnumerators = result.ShufflePlaylistItems();
        }

        result.State = new CollectionEnumeratorState { Seed = state.Seed };
        result._enumeratorIndex = 0;

        // this was a bug when playlist enumerators were first added; shouldn't happen anymore
        if (state.Index < 0)
        {
            state.Index = 0;
        }

        while (result.State.Index < state.Index)
        {
            result.MoveNext();

            // previous state is no longer valid; playlist now has fewer items
            if (result.State.Index == 0)
            {
                break;
            }
        }

        result.ChildEnumerators = enumeratorMap.ToImmutableDictionary();

        return result;
    }

    private List<IMediaCollectionEnumerator> ShufflePlaylistItems()
    {
        if (_sortedEnumerators.Count < 3)
        {
            return _sortedEnumerators;
        }

        IMediaCollectionEnumerator[] copy = _sortedEnumerators.ToArray();
        IMediaCollectionEnumerator last = _sortedEnumerators.Last();

        do
        {
            int n = copy.Length;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                (copy[k], copy[n]) = (copy[n], copy[k]);
            }
        } while (copy.First() == last);

        return copy.ToList();
    }
}
