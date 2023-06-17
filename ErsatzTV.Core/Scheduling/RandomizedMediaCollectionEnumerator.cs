using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public class RandomizedMediaCollectionEnumerator : IMediaCollectionEnumerator
{
    private readonly IList<MediaItem> _mediaItems;
    private readonly Lazy<Option<TimeSpan>> _lazyMinimumDuration;
    private readonly Random _random;
    private int _index;

    public RandomizedMediaCollectionEnumerator(IList<MediaItem> mediaItems, CollectionEnumeratorState state)
    {
        _mediaItems = mediaItems;
        _lazyMinimumDuration =
            new Lazy<Option<TimeSpan>>(() => _mediaItems.Bind(i => i.GetDuration()).OrderBy(identity).HeadOrNone());
        _random = new Random(state.Seed);

        State = new CollectionEnumeratorState { Seed = state.Seed };
        // we want to move at least once so we start with a random item and not the first
        // because _index defaults to 0
        while (State.Index <= state.Index)
        {
            MoveNext();
        }
    }

    public IMediaCollectionEnumerator Clone(CollectionEnumeratorState state, CancellationToken cancellationToken)
    {
        return new RandomizedMediaCollectionEnumerator(_mediaItems, state);
    }

    public CollectionEnumeratorState State { get; }

    public Option<MediaItem> Current => _mediaItems.Any() ? _mediaItems[_index] : None;

    public void MoveNext()
    {
        _index = _random.Next() % _mediaItems.Count;
        State.Index++;
    }

    public Option<TimeSpan> MinimumDuration => _lazyMinimumDuration.Value;
    
    public int Count => _mediaItems.Count;
}
