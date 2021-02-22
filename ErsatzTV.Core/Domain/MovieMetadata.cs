using System;

namespace ErsatzTV.Core.Domain
{
    public class MovieMetadata : MediaItemMetadata
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public MovieMediaItem Movie { get; set; }
        public int? Year { get; set; }
        public DateTime? Premiered { get; set; }
        public string Plot { get; set; }
        public string Outline { get; set; }
        public string Tagline { get; set; }
        public string ContentRating { get; set; }
    }
}
