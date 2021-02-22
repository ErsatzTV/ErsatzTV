using System;

namespace ErsatzTV.Core.Domain
{
    public class TelevisionShowMetadata
    {
        public int Id { get; set; }
        public int TelevisionShowId { get; set; }
        public TelevisionShow TelevisionShow { get; set; }
        public MetadataSource Source { get; set; }
        public DateTime? LastWriteTime { get; set; }
        public string Title { get; set; }
        public string SortTitle { get; set; }
        public int? Year { get; set; }
        public string Plot { get; set; }
    }
}
