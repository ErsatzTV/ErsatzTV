using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public class ShuffledMediaCollectionEnumerator : IMediaCollectionEnumerator
{
    private readonly CancellationToken _cancellationToken;
    private readonly int _mediaItemCount;
    private readonly IList<GroupedMediaItem> _mediaItems;
    private readonly Lazy<Option<TimeSpan>> _lazyMinimumDuration;
    private CloneableRandom _random;
    private IList<MediaItem> _shuffled;

    public ShuffledMediaCollectionEnumerator(
        IList<GroupedMediaItem> mediaItems,
        CollectionEnumeratorState state,
        CancellationToken cancellationToken)
    {
        _mediaItemCount = mediaItems.Sum(i => 1 + Optional(i.Additional).Flatten().Count());
        _mediaItems = mediaItems;
        _cancellationToken = cancellationToken;

        if (state.Index >= _mediaItems.Count)
        {
            state.Index = 0;
            state.Seed = new Random(state.Seed).Next();
        }

        _random = new CloneableRandom(state.Seed);
        _shuffled = Shuffle(_mediaItems, _random);
        _lazyMinimumDuration =
            new Lazy<Option<TimeSpan>>(() => _shuffled.Bind(i => i.GetDuration()).OrderBy(identity).HeadOrNone());

        State = new CollectionEnumeratorState { Seed = state.Seed };
        while (State.Index < state.Index)
        {
            MoveNext();
        }
    }

    public void ResetState(CollectionEnumeratorState state)
    {
        // only re-shuffle if needed
        if (State.Seed != state.Seed)
        {
            _random = new CloneableRandom(state.Seed);
            _shuffled = Shuffle(_mediaItems, _random);
        }

        State.Index = state.Index;
    }

    public CollectionEnumeratorState State { get; }

    public Option<MediaItem> Current => _shuffled.Any() ? _shuffled[State.Index % _mediaItemCount] : None;

    public void MoveNext()
    {
        if ((State.Index + 1) % _mediaItemCount == 0)
        {
            Option<MediaItem> tail = Current;

            State.Index = 0;
            do
            {
                State.Seed = _random.Next();
                _random = new CloneableRandom(State.Seed);
                _shuffled = Shuffle(_mediaItems, _random);
            } while (!_cancellationToken.IsCancellationRequested && _mediaItems.Count > 1 &&
                     Current.Map(x => x.Id) == tail.Map(x => x.Id));
        }
        else
        {
            State.Index++;
        }

        State.Index %= _mediaItemCount;
    }

    private IList<MediaItem> Shuffle(IEnumerable<GroupedMediaItem> list, CloneableRandom random)
    {
        GroupedMediaItem[] copy = list.ToArray();

        int n = copy.Length;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (copy[k], copy[n]) = (copy[n], copy[k]);
        }

        return GroupedMediaItem.FlattenGroups(copy, _mediaItemCount);
    }
    
    public Option<TimeSpan> MinimumDuration => _lazyMinimumDuration.Value;

    public int Count => _shuffled.Count;
}
