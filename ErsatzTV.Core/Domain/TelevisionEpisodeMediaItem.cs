using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class TelevisionEpisodeMediaItem : MediaItem
    {
        public int SeasonId { get; set; }
        public TelevisionSeason Season { get; set; }
        public TelevisionEpisodeMetadata Metadata { get; set; }
        public List<SimpleMediaCollection> SimpleMediaCollections { get; set; }
    }
}
