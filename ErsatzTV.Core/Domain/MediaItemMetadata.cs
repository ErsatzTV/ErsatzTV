using System;

namespace ErsatzTV.Core.Domain
{
    public class MediaItemMetadata
    {
        public MetadataSource Source { get; set; }
        public DateTime? LastWriteTime { get; set; }
        public string Title { get; set; }
        public string SortTitle { get; set; }
    }
}
