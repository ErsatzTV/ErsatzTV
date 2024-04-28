using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public class PlaylistEnumerator : IMediaCollectionEnumerator
{
    private IList<IMediaCollectionEnumerator> _sortedEnumerators;
    private IList<bool> _playAll;
    private int _enumeratorIndex;
    private System.Collections.Generic.HashSet<int> _idsToIncludeInEPG;

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
            _idsToIncludeInEPG = [],
            Count = LCM(playlistItemMap.Values.Map(v => v.Count)) * playlistItemMap.Count
        };

        // collections should share enumerators
        var enumeratorMap = new Dictionary<CollectionKey, IMediaCollectionEnumerator>();

        foreach (PlaylistItem playlistItem in playlistItemMap.Keys.OrderBy(i => i.Index))
        {
            List<MediaItem> items = playlistItemMap[playlistItem];
            if (playlistItem.IncludeInProgramGuide)
            {
                foreach (MediaItem mediaItem in items)
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
                            randomStartPoint: false,
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

        result.MinimumDuration = playlistItemMap.Values
            .Flatten()
            .Bind(i => i.GetNonZeroDuration())
            .OrderBy(identity)
            .HeadOrNone();

        result.State = new CollectionEnumeratorState { Seed = state.Seed };
        result._enumeratorIndex = 0;
        
        // TODO: how do we end up with index > count?
        if (state.Index < result.Count)
        {
            while (result.State.Index < state.Index)
            {
                result.MoveNext();
            }
        }

        return result;
    }

    private PlaylistEnumerator()
    {
    }
    
    public void ResetState(CollectionEnumeratorState state) =>
        // seed doesn't matter here
        State.Index = state.Index;

    public CollectionEnumeratorState State { get; private set; }
    
    public Option<MediaItem> Current => _sortedEnumerators[_enumeratorIndex].Current;
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

    public int Count { get; private set; }

    public Option<TimeSpan> MinimumDuration { get; private set; }

    public void MoveNext()
    {
        _sortedEnumerators[_enumeratorIndex].MoveNext();
        
        // if we aren't playing all, or if we just finished playing all, move to the next enumerator
        if (!_playAll[_enumeratorIndex] || _sortedEnumerators[_enumeratorIndex].State.Index == 0)
        {
            _enumeratorIndex = (_enumeratorIndex + 1) % _sortedEnumerators.Count;
        }

        State.Index = (State.Index + 1) % Count;
    }

    private static int LCM(IEnumerable<int> numbers)
    {
        return numbers.Aggregate(lcm);
    }

    private static int lcm(int a, int b)
    {
        return Math.Abs(a * b) / GCD(a, b);
    }

    private static int GCD(int a, int b)
    {
        while (true)
        {
            if (b == 0) return a;
            int a1 = a;
            a = b;
            b = a1 % b;
        }
    }
}
