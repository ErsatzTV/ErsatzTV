﻿using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class MusicVideo : MediaItem
    {
        // TODO: make this not null
        public int? ArtistId { get; set; }
        public Artist Artist { get; set; }
        public List<MusicVideoMetadata> MusicVideoMetadata { get; set; }
        public List<MediaVersion> MediaVersions { get; set; }
    }
}
