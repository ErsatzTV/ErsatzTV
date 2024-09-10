using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public class RandomizedRotatingMediaCollectionEnumerator : IMediaCollectionEnumerator
{
    private readonly Lazy<Option<TimeSpan>> _lazyMinimumDuration;
    private readonly IList<MediaItem> _mediaItems;
    private readonly Random _random;
    private readonly Dictionary<int, IList<int>> _groupMedia;
    private int _index;
    private int _groupNumber;

    public RandomizedRotatingMediaCollectionEnumerator(IList<MediaItem> mediaItems, CollectionEnumeratorState state)
    {
        CurrentIncludeInProgramGuide = Option<bool>.None;

        _mediaItems = mediaItems;
        _lazyMinimumDuration =
            new Lazy<Option<TimeSpan>>(
                () => _mediaItems.Bind(i => i.GetNonZeroDuration()).OrderBy(identity).HeadOrNone());
        _random = new Random(state.Seed);

        _groupMedia = new Dictionary<int, IList<int>>();
        for (int i = 0; i < mediaItems.Count; i++)
        {
            int id = mediaItems[i] switch
            {
                Episode e => e.Season.ShowId,
                MusicVideo mv => mv.ArtistId,
                _ => mediaItems[i].Id
            };

            if (_groupMedia.TryGetValue(id, out IList<int> newList))
            {
                newList.Add(i);
            }
            else
            {
                _groupMedia.Add(id, new List<int> { i });
            }
        }

        _groupNumber = 0;

        State = new CollectionEnumeratorState { Seed = state.Seed };
        // we want to move at least once so we start with a random item and not the first
        // because _index defaults to 0
        while (State.Index <= state.Index)
        {
            MoveNext();
        }
    }

    public void ResetState(CollectionEnumeratorState state) =>
        // seed never changes here, no need to reset
        State.Index = state.Index;

    public CollectionEnumeratorState State { get; }

    public Option<MediaItem> Current => _mediaItems.Any() ? _mediaItems[_index] : None;
    public Option<bool> CurrentIncludeInProgramGuide { get; }

    public void MoveNext()
    {
        IList<int> groups = _groupMedia.Keys.ToList();
        int nextRandom = _random.Next();

        int groupNumber = nextRandom % groups.Count;
        if (_groupNumber == groupNumber)
        {
            if (groupNumber == groups.Count - 1)
            {
                _groupNumber = 0;
            }
            else
            {
                _groupNumber = groupNumber + 1;
            }
        }
        else
        {
            _groupNumber = groupNumber;
        }

        int itemNumber = nextRandom % _groupMedia[groups[_groupNumber]].Count;
        _index = _groupMedia[groups[_groupNumber]][itemNumber];
        State.Index++;
    }

    public Option<TimeSpan> MinimumDuration => _lazyMinimumDuration.Value;

    public int Count => _mediaItems.Count;
}
