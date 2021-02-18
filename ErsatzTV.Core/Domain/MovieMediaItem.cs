namespace ErsatzTV.Core.Domain
{
    public class MovieMediaItem : MediaItem
    {
        public int MetadataId { get; set; }
        public MovieMetadata Metadata { get; set; }
    }
}
