namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public class ArtistNfo
{
    public ArtistNfo()
    {
        Genres = new List<string>();
        Styles = new List<string>();
        Moods = new List<string>();
    }

    public string? Name { get; set; }
    public string? Disambiguation { get; set; }
    public List<string> Genres { get; }
    public List<string> Styles { get; }
    public List<string> Moods { get; }
    public string? Biography { get; set; }
}
