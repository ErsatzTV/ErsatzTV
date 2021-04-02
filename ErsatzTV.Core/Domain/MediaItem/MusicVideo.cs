using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class MusicVideo : MediaItem
    {
        public List<MusicVideoMetadata> MusicVideoMetadata { get; set; }
        public List<MediaVersion> MediaVersions { get; set; }
    }
}
