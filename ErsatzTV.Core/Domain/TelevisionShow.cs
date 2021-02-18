using System;
using System.Collections.Generic;
using ErsatzTV.Core.Interfaces.Domain;

namespace ErsatzTV.Core.Domain
{
    public class TelevisionShow : IHasAPoster
    {
        public int Id { get; set; }
        public int MediaSourceId { get; set; }
        public MediaSource Source { get; set; }
        public TelevisionShowMetadata Metadata { get; set; }
        public List<TelevisionSeason> Seasons { get; set; }
        public string Path { get; set; }
        public string Poster { get; set; }
        public DateTime? PosterLastWriteTime { get; set; }
    }
}
