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
        private readonly int _mediaItemCount;
        private readonly IList<GroupedMediaItem> _mediaItems;
        private CloneableRandom _random;
        private IList<MediaItem> _shuffled;

        public ShuffledMediaCollectionEnumerator(
            IList<GroupedMediaItem> mediaItems,
            CollectionEnumeratorState state)
        {
            _mediaItemCount = mediaItems.Sum(i => 1 + Optional(i.Additional).Flatten().Count());
            _mediaItems = mediaItems;

            if (state.Index >= _mediaItems.Count)
            {
                state.Index = 0;
                state.Seed = new Random(state.Seed).Next();
            }

            _random = new CloneableRandom(state.Seed);
            _shuffled = Shuffle(_mediaItems, _random);

            State = new CollectionEnumeratorState { Seed = state.Seed };
            while (State.Index < state.Index)
            {
                MoveNext();
            }
        }

        public CollectionEnumeratorState State { get; }

        public Option<MediaItem> Current => _shuffled.Any() ? _shuffled[State.Index % _mediaItemCount] : None;

        public void MoveNext()
        {
            if ((State.Index + 1) % _mediaItemCount == 0)
            {
                Option<MediaItem> tail = Current;

                State.Index = 0;
                do
                {
                    State.Seed = _random.Next();
                    _random = new CloneableRandom(State.Seed);
                    _shuffled = Shuffle(_mediaItems, _random);
                } while (_mediaItems.Count > 1 && Current == tail);
            }
            else
            {
                State.Index++;
            }

            State.Index %= _mediaItemCount;
        }

        public Option<MediaItem> Peek(int offset)
        {
            if (offset == 0)
            {
                return Current;
            }

            if ((State.Index + offset) % _mediaItemCount == 0)
            {
                IList<MediaItem> shuffled;
                Option<MediaItem> tail = Current;

                // clone the random
                CloneableRandom randomCopy = _random.Clone();
                
                do
                {
                    int newSeed = randomCopy.Next();
                    randomCopy = new CloneableRandom(newSeed);
                    shuffled = Shuffle(_mediaItems, randomCopy);
                } while (_mediaItems.Count > 1 && shuffled[0] == tail);

                return shuffled.Any() ? shuffled[0] : None;
            }

            return _shuffled.Any() ? _shuffled[(State.Index + offset) % _mediaItemCount] : None;
        }

        private IList<MediaItem> Shuffle(IEnumerable<GroupedMediaItem> list, CloneableRandom random)
        {
            GroupedMediaItem[] copy = list.ToArray();

            int n = copy.Length;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (copy[k], copy[n]) = (copy[n], copy[k]);
            }

            return GroupedMediaItem.FlattenGroups(copy, _mediaItemCount);
        }
    }
}
