﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public sealed class SeasonEpisodeMediaCollectionEnumerator : IMediaCollectionEnumerator
{
    private readonly IList<MediaItem> _sortedMediaItems;

    public SeasonEpisodeMediaCollectionEnumerator(
        IEnumerable<MediaItem> mediaItems,
        CollectionEnumeratorState state)
    {
        _sortedMediaItems = mediaItems.OrderBy(identity, new SeasonEpisodeMediaComparer()).ToList();

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

    public CollectionEnumeratorState State { get; }

    public Option<MediaItem> Current => _sortedMediaItems.Any() ? _sortedMediaItems[State.Index] : None;

    public void MoveNext() => State.Index = (State.Index + 1) % _sortedMediaItems.Count;

    public Option<MediaItem> Peek(int offset) =>
        _sortedMediaItems.Any() ? _sortedMediaItems[(State.Index + offset) % _sortedMediaItems.Count] : None;
}
