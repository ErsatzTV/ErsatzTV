namespace ErsatzTV.Core.Scheduling;

public static class MultiCollectionGrouper
{
    public static List<GroupedMediaItem> GroupMediaItems(IEnumerable<CollectionWithItems> collections)
    {
        var result = new List<GroupedMediaItem>();

        foreach (CollectionWithItems collection in collections.Where(collection => collection.MediaItems.Any()))
        {
            if (collection.ScheduleAsGroup)
            {
                result.Add(new MultiCollectionGroup(collection));
            }
            else
            {
                result.AddRange(collection.MediaItems.Map(i => new GroupedMediaItem { First = i }));
            }
        }

        return result.Distinct().ToList();
    }
}
