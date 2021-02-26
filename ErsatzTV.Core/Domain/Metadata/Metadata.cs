using System;

namespace ErsatzTV.Core.Domain
{
    public class Metadata
    {
        public int Id { get; set; }
        public MetadataKind MetadataKind { get; set; }
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public string SortTitle { get; set; }
        public DateTimeOffset? ReleaseDate { get; set; }
        public DateTimeOffset DateAdded { get; set; }
        public DateTimeOffset DateUpdated { get; set; }        
    }
}
