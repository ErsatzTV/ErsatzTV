using System;
using System.Collections.Generic;
using ErsatzTV.Core.Interfaces.Domain;

namespace ErsatzTV.Core.Domain
{
    public class MediaItem : IHasAPoster
    {
        public int Id { get; set; }
        public MediaItemStatistics Statistics { get; set; }
        public DateTime? LastWriteTime { get; set; }

        public int LibraryPathId { get; set; }
        public LibraryPath LibraryPath { get; set; }

        // temporary fields to help migrations...
        public int TelevisionShowId { get; set; }
        public int TelevisionSeasonId { get; set; }
        public int TelevisionEpisodeId { get; set; }

        public List<Collection> Collections { get; set; }
        public List<CollectionItem> CollectionItems { get; set; }
        public string Path { get; set; }
        public string Poster { get; set; }
        public DateTime? PosterLastWriteTime { get; set; }
    }
}
