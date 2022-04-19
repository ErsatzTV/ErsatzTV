using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling;

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

    public Option<MediaItem> Peek(int offset) => throw new NotSupportedException();

    private IList<MediaItem> Shuffle(IList<CollectionWithItems> collections, Random random)
    {
        // based on https://keyj.emphy.de/balanced-shuffle/

        var orderedCollections = collections
            .Filter(c => c.ScheduleAsGroup)
            .Map(c => new OrderedCollection { Index = 0, Items = OrderItems(c) })
            .ToList();

        if (collections.Any(c => !c.ScheduleAsGroup))
        {
            orderedCollections.Add(
                new OrderedCollection
                {
                    Index = 0,
                    Items = Shuffle(
                        collections.Filter(c => !c.ScheduleAsGroup).SelectMany(c => c.MediaItems.Map(Some)),
                        random)
                });
        }

        List<OrderedCollection> filled = Fill(orderedCollections, random);

        var result = new List<MediaItem>();
        for (var i = 0; i < filled[0].Items.Count; i++)
        {
            var batch = filled.Select(collection => collection.Items[i]).ToList();
            foreach (Option<MediaItem> maybeItem in Shuffle(batch, random))
            {
                result.AddRange(maybeItem);
            }
        }

        return result;
    }

    private List<OrderedCollection> Fill(List<OrderedCollection> orderedCollections, Random random)
    {
        var result = new List<OrderedCollection>();
        int maxLength = orderedCollections.Max(c => c.Items.Count);

        foreach (OrderedCollection collection in orderedCollections)
        {
            var items = new Queue<Option<MediaItem>>(collection.Items);
            var spaces = new Queue<Option<MediaItem>>(
                Range(0, maxLength - collection.Items.Count).Map(_ => Option<MediaItem>.None).ToList());

            Queue<Option<MediaItem>> smaller = collection.Items.Count < maxLength - collection.Items.Count
                ? items
                : spaces;
            Queue<Option<MediaItem>> larger = collection.Items.Count < maxLength - collection.Items.Count
                ? spaces
                : items;

            var ordered = new List<Option<MediaItem>>();

            int k = smaller.Count;
            while (k > 0)
            {
                int n = maxLength - ordered.Count;

                // compute optimal length +/- 10%
                double optimalLength = n / (double)k + (random.NextDouble() - 0.5) / 5.0;
                int r = Math.Clamp((int)optimalLength, 1, maxLength - k + 1);
                ordered.Add(smaller.Dequeue());
                for (var i = 0; i < r - 1; i++)
                {
                    ordered.Add(larger.Dequeue());
                }

                k--;
            }

            if (smaller.Any())
            {
                ordered.AddRange(smaller);
            }

            if (larger.Any())
            {
                ordered.AddRange(larger);
            }

            result.Add(new OrderedCollection { Index = 0, Items = ordered });
        }

        return result;
    }

    private static IList<Option<MediaItem>> OrderItems(CollectionWithItems collectionWithItems)
    {
        if (collectionWithItems.UseCustomOrder)
        {
            return collectionWithItems.MediaItems.Map(Some).ToList();
        }

        return collectionWithItems.MediaItems
            .OrderBy(identity, new ChronologicalMediaComparer())
            .Map(Some)
            .ToList();
    }

    private static IList<Option<MediaItem>> Shuffle(IEnumerable<Option<MediaItem>> list, Random random)
    {
        Option<MediaItem>[] copy = list.ToArray();

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
        public IList<Option<MediaItem>> Items { get; set; }
    }
}
