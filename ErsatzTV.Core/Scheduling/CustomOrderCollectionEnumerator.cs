using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling;

public class CustomOrderCollectionEnumerator : IMediaCollectionEnumerator
{
    private readonly Collection _collection;
    private readonly IList<MediaItem> _sortedMediaItems;

    public CustomOrderCollectionEnumerator(
        Collection collection,
        IList<MediaItem> mediaItems,
        CollectionEnumeratorState state)
    {
        _collection = collection;

        // TODO: this will break if we allow shows and seasons
        _sortedMediaItems = collection.CollectionItems
            .OrderBy(ci => ci.CustomIndex)
            .Map(ci => mediaItems.First(mi => mi.Id == ci.MediaItemId))
            .ToList();

        State = new CollectionEnumeratorState { Seed = state.Seed };
        while (State.Index < state.Index)
        {
            MoveNext();
        }
    }

    public CollectionEnumeratorState State { get; }

    public Option<MediaItem> Current => _sortedMediaItems.Any() ? _sortedMediaItems[State.Index] : None;

    public void MoveNext() => State.Index = (State.Index + 1) % _sortedMediaItems.Count;

    public Option<MediaItem> Peek(int offset) =>
        throw new NotSupportedException();
}