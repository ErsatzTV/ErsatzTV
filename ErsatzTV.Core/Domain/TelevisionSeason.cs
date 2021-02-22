using System;
using System.Collections.Generic;
using ErsatzTV.Core.Interfaces.Domain;

namespace ErsatzTV.Core.Domain
{
    public class TelevisionSeason : IHasAPoster
    {
        public int Id { get; set; }
        public int TelevisionShowId { get; set; }
        public TelevisionShow TelevisionShow { get; set; }
        public int Number { get; set; }
        public List<TelevisionEpisodeMediaItem> Episodes { get; set; }
        public List<SimpleMediaCollection> SimpleMediaCollections { get; set; }
        public string Path { get; set; }
        public string Poster { get; set; }
        public DateTime? PosterLastWriteTime { get; set; }
    }
}
