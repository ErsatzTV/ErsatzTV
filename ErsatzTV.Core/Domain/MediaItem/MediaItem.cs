using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class MediaItem
    {
        public int Id { get; set; }
        public int LibraryPathId { get; set; }
        public LibraryPath LibraryPath { get; set; }
        public List<Collection> Collections { get; set; }
        public List<CollectionItem> CollectionItems { get; set; }
        public List<TraktListItem> TraktListItems { get; set; }
    }
}
