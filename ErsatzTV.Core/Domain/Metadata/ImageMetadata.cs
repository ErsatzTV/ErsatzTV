namespace ErsatzTV.Core.Domain;

public class ImageMetadata : Metadata
{
    public int? DurationSeconds { get; set; }
    public int ImageId { get; set; }
    public Image Image { get; set; }
}
