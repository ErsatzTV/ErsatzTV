using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class SimpleMediaCollection : MediaCollection
    {
        public List<MovieMediaItem> Movies { get; set; }
        public List<TelevisionShow> TelevisionShows { get; set; }
        public List<TelevisionSeason> TelevisionSeasons { get; set; }
        public List<TelevisionEpisodeMediaItem> TelevisionEpisodes { get; set; }
    }
}
