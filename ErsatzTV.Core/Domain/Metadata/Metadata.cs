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
        public DateTime? ReleaseDate { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
