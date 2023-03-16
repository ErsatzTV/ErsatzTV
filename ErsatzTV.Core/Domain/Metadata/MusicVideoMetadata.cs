namespace ErsatzTV.Core.Domain;

public class MusicVideoMetadata : Metadata
{
    public string Album { get; set; }
    public string Plot { get; set; }
    public int? Track { get; set; }
    public int MusicVideoId { get; set; }
    public MusicVideo MusicVideo { get; set; }
    public List<MusicVideoArtist> Artists { get; set; }
    public List<Director> Directors { get; set; }
}
