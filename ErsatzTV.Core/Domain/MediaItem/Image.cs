namespace ErsatzTV.Core.Domain;

public class Image : MediaItem
{
    public static readonly int DefaultSeconds = 15;
    
    public List<ImageMetadata> ImageMetadata { get; set; }
    public List<MediaVersion> MediaVersions { get; set; }
}
