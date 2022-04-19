namespace ErsatzTV.Core.Domain;

public class OtherVideo : MediaItem
{
    public List<OtherVideoMetadata> OtherVideoMetadata { get; set; }
    public List<MediaVersion> MediaVersions { get; set; }
}
