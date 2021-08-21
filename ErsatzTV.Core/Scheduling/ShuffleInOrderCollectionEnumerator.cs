using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class ShuffleInOrderCollectionEnumerator : IMediaCollectionEnumerator
    {
        private readonly IList<CollectionWithItems> _collections;
        private readonly int _mediaItemCount;
        private Random _random;
        private IList<MediaItem> _shuffled;

        public ShuffleInOrderCollectionEnumerator(
            IList<CollectionWithItems> collections,
            CollectionEnumeratorState state)
        {
            _collections = collections;
            _mediaItemCount = collections.Sum(c => c.MediaItems.Count);

            if (state.Index >= _mediaItemCount)
            {
                state.Index = 0;
                state.Seed = new Random(state.Seed).Next();
            }
            
            _random = new Random(state.Seed);
            _shuffled = Shuffle(_collections, _random);
            
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
            if ((State.Index + 1) % _shuffled.Count == 0)
            {
                Option<MediaItem> tail = Current;

                State.Index = 0;
                do
                {
                    State.Seed = _random.Next();
                    _random = new Random(State.Seed);
                    _shuffled = Shuffle(_collections, _random);
                } while (_collections.Count > 1 && Current == tail);
            }
            else
            {
                State.Index++;
            }

            State.Index %= _shuffled.Count;
        }
        
        private IList<MediaItem> Shuffle(IList<CollectionWithItems> collections, Random random)
        {
            // TODO: "balanced" option?
            
            var orderedCollections = collections
                .Filter(c => c.ScheduleAsGroup)
                .Map(c => new OrderedCollection { Index = 0, Items = OrderItems(c) })
                .ToList();

            orderedCollections.Add(
                new OrderedCollection
                {
                    Index = 0,
                    Items = Shuffle(collections.Filter(c => !c.ScheduleAsGroup).SelectMany(c => c.MediaItems), random)
                });

            var result = new MediaItem[_mediaItemCount];
            
            var i = 0;
            while (i < result.Length)
            {
                int takeFrom = random.Next(orderedCollections.Count);
                OrderedCollection target = orderedCollections[takeFrom];
                if (target.Index >= target.Items.Count)
                {
                    continue;
                }

                result[i] = target.Items[target.Index];
                target.Index += 1;
                i += 1;
            }

            return result;
        }
        
        private static IList<MediaItem> OrderItems(CollectionWithItems collectionWithItems)
        {
            if (collectionWithItems.UseCustomOrder)
            {
                return collectionWithItems.MediaItems;
            }
            
            return collectionWithItems.MediaItems
                .OrderBy(identity, new ChronologicalMediaComparer())
                .ToList();
        }
        
        private static IList<MediaItem> Shuffle(IEnumerable<MediaItem> list, Random random)
        {
            MediaItem[] copy = list.ToArray();

            int n = copy.Length;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (copy[k], copy[n]) = (copy[n], copy[k]);
            }

            return copy;
        }

        private class OrderedCollection
        {
            public int Index { get; set; }
            public IList<MediaItem> Items { get; set; }
        }
    }
}
