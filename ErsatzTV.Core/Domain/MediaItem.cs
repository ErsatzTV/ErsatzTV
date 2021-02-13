using System;
using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class MediaItem
    {
        public int Id { get; set; }
        public int MediaSourceId { get; set; }
        public MediaSource Source { get; set; }
        public string Path { get; set; }
        public string PosterPath { get; set; }
        public MediaMetadata Metadata { get; set; }
        public DateTime? LastWriteTime { get; set; }
        public IList<SimpleMediaCollection> SimpleMediaCollections { get; set; }
    }
}
