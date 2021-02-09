using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class ShuffledMediaCollectionEnumerator : IMediaCollectionEnumerator
    {
        private readonly IList<MediaItem> _mediaItems;
        private Option<int> _peekNextSeed;
        private Random _random;
        private IList<MediaItem> _shuffled;


        public ShuffledMediaCollectionEnumerator(IList<MediaItem> mediaItems, MediaCollectionEnumeratorState state)
        {
            _mediaItems = mediaItems;
            _random = new Random(state.Seed);
            _shuffled = Shuffle(_mediaItems, _random);

            State = new MediaCollectionEnumeratorState { Seed = state.Seed };
            while (State.Index < state.Index)
            {
                MoveNext();
            }
        }

        public MediaCollectionEnumeratorState State { get; }

        public Option<MediaItem> Current => _shuffled.Any() ? _shuffled[State.Index % _mediaItems.Count] : None;

        public Option<MediaItem> Peek
        {
            get
            {
                if (_shuffled.Any())
                {
                    // if we aren't peeking past the end of the list, things are simple
                    if (State.Index + 1 < _shuffled.Count)
                    {
                        return _shuffled[State.Index + 1];
                    }

                    // if we are peeking past the end of the list...
                    // gen a random seed but save it so we can use it again when we actually move next
                    Random random;
                    if (_peekNextSeed.IsSome)
                    {
                        random = new Random(_peekNextSeed.Value());
                    }
                    else
                    {
                        _peekNextSeed = _random.Next();
                        random = new Random(_peekNextSeed.Value());
                    }

                    return Shuffle(_mediaItems, random).Head();
                }

                return None;
            }
        }

        public void MoveNext()
        {
            State.Index++;
            if (State.Index % _shuffled.Count == 0)
            {
                State.Index = 0;
                State.Seed = _peekNextSeed.IfNone(_random.Next());
                _random = new Random(State.Seed);
                _shuffled = Shuffle(_mediaItems, _random);
            }

            State.Index %= _shuffled.Count;
            _peekNextSeed = None;
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
