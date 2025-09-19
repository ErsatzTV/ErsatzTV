using System.Collections;

namespace ErsatzTV.Core.Extensions;

public static class IEnumerableExtensions
{
    public static IEnumerable<IGrouping<TKey, TSource>> GroupConsecutiveBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        EqualityComparer<TKey> comparer = EqualityComparer<TKey>.Default;

        List<TSource> currentGroup = null;

        foreach (TSource item in source)
        {
            TKey key = keySelector(item);

            if (currentGroup == null)
            {
                currentGroup = [item];
                continue;
            }

            if (comparer.Equals(keySelector(currentGroup[0]), key))
            {
                currentGroup.Add(item);
            }
            else
            {
                yield return new Grouping<TKey, TSource>(keySelector(currentGroup[0]), currentGroup);
                currentGroup = [item];
            }
        }

        if (currentGroup != null)
        {
            yield return new Grouping<TKey, TSource>(keySelector(currentGroup[0]), currentGroup);
        }
    }

    private class Grouping<TKey, TElement>(TKey key, IEnumerable<TElement> elements) : IGrouping<TKey, TElement>
    {
        public TKey Key { get; } = key;
        public IEnumerator<TElement> GetEnumerator() => elements.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
