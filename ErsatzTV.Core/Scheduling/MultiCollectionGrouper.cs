using System.Collections.Generic;

namespace ErsatzTV.Core.Scheduling
{
    public class MultiCollectionGrouper
    {
        public static List<GroupedMediaItem> GroupMediaItems(IList<CollectionWithItems> collections)
        {
            var result = new List<GroupedMediaItem>();

            foreach (CollectionWithItems collection in collections)
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

            return result;
        }
    }
}
