using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class ShuffledMediaCollectionEnumerator : IMediaCollectionEnumerator
    {
        private readonly IList<MediaItem> _mediaItems;
        private Random _random;
        private IList<MediaItem> _shuffled;


        public ShuffledMediaCollectionEnumerator(IList<MediaItem> mediaItems, CollectionEnumeratorState state)
        {
            _mediaItems = mediaItems;
            _random = new Random(state.Seed);
            _shuffled = Shuffle(_mediaItems, _random);

            State = new CollectionEnumeratorState { Seed = state.Seed };
            while (State.Index < state.Index)
            {
                MoveNext();
            }
        }

        public CollectionEnumeratorState State { get; }

        public Option<MediaItem> Current => _shuffled.Any() ? _shuffled[State.Index % _mediaItems.Count] : None;

        public void MoveNext()
        {
            if ((State.Index + 1) % _shuffled.Count == 0)
            {
                Option<MediaItem> tail = Current;

                State.Index = 0;
                do
                {
                    State.Seed = _random.Next();
                    _random = new Random(State.Seed);
                    _shuffled = Shuffle(_mediaItems, _random);
                } while (_mediaItems.Count > 1 && Current == tail);
            }
            else
            {
                State.Index++;
            }

            State.Index %= _shuffled.Count;
        }

        private static IList<T> Shuffle<T>(IEnumerable<T> list, Random random)
        {
            T[] copy = list.ToArray();

            int n = copy.Length;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = copy[k];
                copy[k] = copy[n];
                copy[n] = value;
            }

            return copy;
        }
    }
}
