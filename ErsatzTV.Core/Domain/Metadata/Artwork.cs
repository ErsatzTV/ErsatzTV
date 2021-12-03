using System;

namespace ErsatzTV.Core.Domain
{
    public class Artwork
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string BlurHash43 { get; set; }
        public string BlurHash54 { get; set; }
        public string BlurHash64 { get; set; }
        public ArtworkKind ArtworkKind { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
