﻿namespace ErsatzTV.Core.Domain
{
    public class EpisodeMetadata : Metadata
    {
        public string Outline { get; set; }
        public string Plot { get; set; }
        public string Tagline { get; set; }
        public int EpisodeId { get; set; }
        public Episode Episode { get; set; }
    }
}
