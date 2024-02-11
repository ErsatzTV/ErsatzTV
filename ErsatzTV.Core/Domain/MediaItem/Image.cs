namespace ErsatzTV.Core.Domain;

public class Image : MediaItem
{
    public List<ImageMetadata> ImageMetadata { get; set; }
    public List<MediaVersion> MediaVersions { get; set; }
}
