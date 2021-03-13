namespace ErsatzTV.Infrastructure.Plex.Models
{
    public class PlexStreamResponse
    {
        public int Id { get; set; }
        public int StreamType { get; set; }
        public bool Anamorphic { get; set; }
        public string PixelAspectRatio { get; set; }
        public string ScanType { get; set; }
    }
}
