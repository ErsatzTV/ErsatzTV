namespace ErsatzTV.Core.Domain;

public class SongMetadata : Metadata
{
    public string Album { get; set; }
    public string Artist { get; set; }
    public string AlbumArtist { get; set; }
    public string Date { get; set; }
    public string Track { get; set; }
    public int SongId { get; set; }
    public Song Song { get; set; }
}
