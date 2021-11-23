namespace ErsatzTV.Core.Domain
{
    public class SongMetadata : Metadata
    {
        public int SongId { get; set; }
        public Song Song { get; set; }
    }
}
