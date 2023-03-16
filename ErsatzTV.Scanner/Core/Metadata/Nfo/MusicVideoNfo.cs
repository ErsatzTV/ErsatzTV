namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public class MusicVideoNfo
{
    public MusicVideoNfo()
    {
        Artists = new List<string>();
        Genres = new List<string>();
        Tags = new List<string>();
        Studios = new List<string>();
        Directors = new List<string>();
    }

    public List<string> Artists { get; }
    public string? Title { get; set; }
    public string? Album { get; set; }
    public string? Plot { get; set; }
    public int Track { get; set; }
    public Option<DateTime> Aired { get; set; }
    public int Year { get; set; }
    public List<string> Genres { get; }
    public List<string> Tags { get; }
    public List<string> Studios { get; }
    public List<string> Directors { get; }
}
