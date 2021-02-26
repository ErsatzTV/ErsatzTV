using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class Collection
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<MediaItem> MediaItems { get; set; }
        public List<CollectionItem> CollectionItems { get; set; }
    }
}
