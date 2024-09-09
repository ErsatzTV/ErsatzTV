using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public sealed class LatestMediaCollectionEnumerator : IMediaCollectionEnumerator
{
    private readonly Lazy<Option<TimeSpan>> _lazyMinimumDuration;
    private readonly IList<MediaItem> _sortedMediaItems;

    public LatestMediaCollectionEnumerator(
        IEnumerable<MediaItem> mediaItems,
        CollectionEnumeratorState state)
    {
        CurrentIncludeInProgramGuide = Option<bool>.None;

        _sortedMediaItems = mediaItems.OrderBy(identity, new ChronologicalMediaComparer()).ToList();
        _lazyMinimumDuration = new Lazy<Option<TimeSpan>>(
            () => _sortedMediaItems.Bind(i => i.GetNonZeroDuration()).OrderBy(identity).HeadOrNone());

        State = new CollectionEnumeratorState { Seed = state.Seed };

        if (state.Index >= _sortedMediaItems.Count)
        {
            state.Index = 0;
            state.Seed = 0;
        }

        State.Index = _sortedMediaItems.Count - 1;

        //while (State.Index < state.Index)
        //{
        //    MoveNext();
        //}
    }

    public void ResetState(CollectionEnumeratorState state) =>
        // seed doesn't matter in chronological
        State.Index = state.Index;

    public CollectionEnumeratorState State { get; }

    public Option<MediaItem> Current => _sortedMediaItems.Any() ? _sortedMediaItems[State.Index] : None;
    public Option<bool> CurrentIncludeInProgramGuide { get; }

    public void MoveNext() => State.Index = _sortedMediaItems.Count - 1;

    public Option<TimeSpan> MinimumDuration => _lazyMinimumDuration.Value;

    public int Count => _sortedMediaItems.Count;
}
