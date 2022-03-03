namespace ErsatzTV.Core.Domain;

public class Song : MediaItem
{
    public List<SongMetadata> SongMetadata { get; set; }
    public List<MediaVersion> MediaVersions { get; set; }
}