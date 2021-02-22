using System;

namespace ErsatzTV.Core.Domain
{
    public class TelevisionEpisodeMetadata : MediaItemMetadata
    {
        public int Id { get; set; }
        public int TelevisionEpisodeId { get; set; }
        public TelevisionEpisodeMediaItem TelevisionEpisode { get; set; }
        public int Season { get; set; }
        public int Episode { get; set; }
        public string Plot { get; set; }
        public DateTime? Aired { get; set; }
    }
}
