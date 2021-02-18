using System;
using System.Collections.Generic;
using ErsatzTV.Core.Interfaces.Domain;

namespace ErsatzTV.Core.Domain
{
    public class MediaItem : IHasAPoster
    {
        public int Id { get; set; }
        public int MediaSourceId { get; set; }

        public MediaSource Source { get; set; }

        // public MediaMetadata Metadata { get; set; }
        public MediaItemStatistics Statistics { get; set; }
        public DateTime? LastWriteTime { get; set; }
        public IList<SimpleMediaCollection> SimpleMediaCollections { get; set; }
        public string Path { get; set; }
        public string Poster { get; set; }
        public DateTime? PosterLastWriteTime { get; set; }
    }
}
