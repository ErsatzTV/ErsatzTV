using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class RandomizedMediaCollectionEnumerator : IMediaCollectionEnumerator
    {
        private readonly IList<MediaItem> _mediaItems;
        private readonly Random _random;
        private int _index;

        public RandomizedMediaCollectionEnumerator(IList<MediaItem> mediaItems, MediaCollectionEnumeratorState state)
        {
            _mediaItems = mediaItems;
            _random = new Random(state.Seed);

            State = new MediaCollectionEnumeratorState { Seed = state.Seed };
            // we want to move at least once so we start with a random item and not the first
            // because _index defaults to 0
            while (State.Index <= state.Index)
            {
                MoveNext();
            }
        }

        public MediaCollectionEnumeratorState State { get; }

        public Option<MediaItem> Current => _mediaItems.Any() ? _mediaItems[_index] : None;

        public void MoveNext()
        {
            _index = _random.Next() % _mediaItems.Count;
            State.Index++;
        }
    }
}
