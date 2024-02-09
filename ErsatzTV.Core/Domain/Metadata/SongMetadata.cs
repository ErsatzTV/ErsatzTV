namespace ErsatzTV.Core.Domain;

public class SongMetadata : Metadata
{
    public string Album { get; set; }
    public IList<string> Artists { get; set; }
    public IList<string> AlbumArtists { get; set; }
    public string Track { get; set; }
    public string Comment { get; set; }
    public int SongId { get; set; }
    public Song Song { get; set; }
}
