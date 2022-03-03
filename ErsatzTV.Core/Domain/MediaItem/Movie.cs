namespace ErsatzTV.Core.Domain;

public class Movie : MediaItem
{
    public List<MovieMetadata> MovieMetadata { get; set; }
    public List<MediaVersion> MediaVersions { get; set; }
}