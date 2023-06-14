using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public class CustomOrderCollectionEnumerator : IMediaCollectionEnumerator
{
    private readonly Collection _collection;
    private readonly IList<MediaItem> _mediaItems;

    private readonly IList<MediaItem> _sortedMediaItems;
    private readonly Lazy<Option<TimeSpan>> _lazyMinimumDuration;

    public CustomOrderCollectionEnumerator(
        Collection collection,
        IList<MediaItem> mediaItems,
        CollectionEnumeratorState state)
    {
        _collection = collection;
        _mediaItems = mediaItems;

        // TODO: this will break if we allow shows and seasons
        _sortedMediaItems = collection.CollectionItems
            .OrderBy(ci => ci.CustomIndex)
            .Map(ci => mediaItems.First(mi => mi.Id == ci.MediaItemId))
            .ToList();
        _lazyMinimumDuration = new Lazy<Option<TimeSpan>>(
            () => _sortedMediaItems.Bind(i => i.GetDuration()).HeadOrNone());

        State = new CollectionEnumeratorState { Seed = state.Seed };
        while (State.Index < state.Index)
        {
            MoveNext();
        }
    }

    public IMediaCollectionEnumerator Clone(CollectionEnumeratorState state, CancellationToken cancellationToken)
    {
        return new CustomOrderCollectionEnumerator(_collection, _mediaItems, state);
    }

    public CollectionEnumeratorState State { get; }

    public Option<MediaItem> Current => _sortedMediaItems.Any() ? _sortedMediaItems[State.Index] : None;

    public void MoveNext() => State.Index = (State.Index + 1) % _sortedMediaItems.Count;

    public Option<TimeSpan> MinimumDuration => _lazyMinimumDuration.Value;
    
    public int Count => _sortedMediaItems.Count;
}
