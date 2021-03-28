namespace ErsatzTV.Infrastructure.Plex.Models
{
    public class PlexStreamResponse
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public bool Default { get; set; }
        public bool Forced { get; set; }
        public string LanguageCode { get; set; }
        public int StreamType { get; set; }
        public string Codec { get; set; }
        public string Profile { get; set; }
        public int Channels { get; set; }
        public bool Anamorphic { get; set; }
        public string PixelAspectRatio { get; set; }
        public string ScanType { get; set; }
    }
}
