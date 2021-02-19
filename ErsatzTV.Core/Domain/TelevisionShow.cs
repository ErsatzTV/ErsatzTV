using System;
using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class TelevisionShow
    {
        public int Id { get; set; }
        public List<TelevisionShowSource> Sources { get; set; }
        public TelevisionShowMetadata Metadata { get; set; }
        public List<TelevisionSeason> Seasons { get; set; }
        public string Poster { get; set; }
        public DateTime? PosterLastWriteTime { get; set; }
    }
}
