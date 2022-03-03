namespace ErsatzTV.Core.Domain;

public class MusicVideo : MediaItem
{
    public int ArtistId { get; set; }
    public Artist Artist { get; set; }
    public List<MusicVideoMetadata> MusicVideoMetadata { get; set; }
    public List<MediaVersion> MediaVersions { get; set; }
}