﻿using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public sealed class ChronologicalMediaCollectionEnumerator : IMediaCollectionEnumerator
    {
        private readonly IList<MediaItem> _sortedMediaItems;

        public ChronologicalMediaCollectionEnumerator(
            IEnumerable<MediaItem> mediaItems,
            CollectionEnumeratorState state)
        {
            _sortedMediaItems = mediaItems.OrderBy(identity, new ChronologicalMediaComparer()).ToList();

            State = new CollectionEnumeratorState { Seed = state.Seed };
            while (State.Index < state.Index)
            {
                MoveNext();
            }
        }

        public CollectionEnumeratorState State { get; }

        public Option<MediaItem> Current => _sortedMediaItems.Any() ? _sortedMediaItems[State.Index] : None;

        public void MoveNext() => State.Index = (State.Index + 1) % _sortedMediaItems.Count;
    }
}
