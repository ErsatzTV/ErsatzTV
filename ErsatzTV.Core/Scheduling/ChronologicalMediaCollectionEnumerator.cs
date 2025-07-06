using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public sealed class ChronologicalMediaCollectionEnumerator : IMediaCollectionEnumerator
{
    private readonly Lazy<Option<TimeSpan>> _lazyMinimumDuration;
    private readonly List<MediaItem> _sortedMediaItems;

    public ChronologicalMediaCollectionEnumerator(
        IEnumerable<MediaItem> mediaItems,
        CollectionEnumeratorState state)
    {
        CurrentIncludeInProgramGuide = Option<bool>.None;

        _sortedMediaItems = mediaItems.OrderBy(identity, new ChronologicalMediaComparer()).ToList();
        _lazyMinimumDuration = new Lazy<Option<TimeSpan>>(() =>
            _sortedMediaItems.Bind(i => i.GetNonZeroDuration()).OrderBy(identity).HeadOrNone());

        State = new CollectionEnumeratorState { Seed = state.Seed };

        if (state.Index >= _sortedMediaItems.Count)
        {
            state.Index = 0;
            state.Seed = 0;
        }

        while (State.Index < state.Index)
        {
            MoveNext();
        }
    }

    public void ResetState(CollectionEnumeratorState state) =>
        // seed doesn't matter in chronological
        State.Index = state.Index;

    public CollectionEnumeratorState State { get; }

    public Option<MediaItem> Current => _sortedMediaItems.Count != 0 ? _sortedMediaItems[State.Index] : None;
    public Option<bool> CurrentIncludeInProgramGuide { get; }

    public void MoveNext() => State.Index = (State.Index + 1) % _sortedMediaItems.Count;

    public Option<TimeSpan> MinimumDuration => _lazyMinimumDuration.Value;

    public int Count => _sortedMediaItems.Count;
}
