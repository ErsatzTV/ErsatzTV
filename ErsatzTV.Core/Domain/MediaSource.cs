using System;

namespace ErsatzTV.Core.Domain
{
    public abstract class MediaSource
    {
        public int Id { get; set; }
        public MediaSourceType SourceType { get; set; }
        public string Name { get; set; }
        public DateTimeOffset? LastScan { get; set; }
    }
}
