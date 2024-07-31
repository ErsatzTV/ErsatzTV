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
    private IList<IMediaCollectionEnumerator> _sortedEnumerators;

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
        }
    }

    public static async Task<PlaylistEnumerator> Create(
        IMediaCollectionRepository mediaCollectionRepository,
        Dictionary<PlaylistItem, List<MediaItem>> playlistItemMap,
        CollectionEnumeratorState state,
        CancellationToken cancellationToken)
    {
        var result = new PlaylistEnumerator
        {
            _sortedEnumerators = [],
            _playAll = [],
            _idsToIncludeInEPG = []
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
        }

        return result;
    }
}
