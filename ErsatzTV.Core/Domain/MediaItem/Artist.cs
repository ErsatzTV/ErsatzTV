using System.Collections.Generic;

namespace ErsatzTV.Core.Domain;

public class Artist : MediaItem
{
    public List<MusicVideo> MusicVideos { get; set; }
    public List<ArtistMetadata> ArtistMetadata { get; set; }
}