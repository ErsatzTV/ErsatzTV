using System.Collections.Generic;
using System.Diagnostics;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    [DebuggerDisplay("{" + nameof(First) + "}")]
    public class GroupedMediaItem
    {
        public GroupedMediaItem()
        {
        }

        public GroupedMediaItem(MediaItem first, List<MediaItem> additional)
        {
            First = first;
            Additional = additional ?? new List<MediaItem>();
        }

        public MediaItem First { get; set; }
        public List<MediaItem> Additional { get; set; }
        
        public static IList<MediaItem> FlattenGroups(GroupedMediaItem[] copy, int mediaItemCount)
        {
            var result = new MediaItem[mediaItemCount];
            var i = 0;
            foreach (GroupedMediaItem group in copy)
            {
                result[i++] = group.First;
                foreach (MediaItem additional in Optional(group.Additional).Flatten())
                {
                    result[i++] = additional;
                }
            }

            return result;
        }
    }
}
