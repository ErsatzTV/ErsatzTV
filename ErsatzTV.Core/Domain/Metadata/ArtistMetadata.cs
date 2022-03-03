namespace ErsatzTV.Core.Domain;

public class ArtistMetadata : Metadata
{
    public string Disambiguation { get; set; }
    public string Biography { get; set; }
    public string Formed { get; set; }
    public int ArtistId { get; set; }
    public Artist Artist { get; set; }
    public List<Style> Styles { get; set; }
    public List<Mood> Moods { get; set; }
}