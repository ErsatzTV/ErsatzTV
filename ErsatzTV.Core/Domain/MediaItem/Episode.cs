using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class Episode : MediaItem
    {
        public int EpisodeNumber { get; set; }
        public int SeasonId { get; set; }
        public Season Season { get; set; }
        public List<EpisodeMetadata> EpisodeMetadata { get; set; }
        public List<MediaVersion> MediaVersions { get; set; }
    }
}
