namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public class ShowNfo
{
    public ShowNfo()
    {
        Genres = new List<string>();
        Tags = new List<string>();
        Studios = new List<string>();
        Actors = new List<ActorNfo>();
        UniqueIds = new List<UniqueIdNfo>();
    }

    public string? Title { get; set; }
    public int? Year { get; set; }
    public string? Plot { get; set; }
    public string? Outline { get; set; }
    public string? Tagline { get; set; }
    public string? ContentRating { get; set; }
    public Option<DateTime> Premiered { get; set; }
    public List<string> Genres { get; }
    public List<string> Tags { get; }
    public List<string> Studios { get; }
    public List<ActorNfo> Actors { get; }
    public List<UniqueIdNfo> UniqueIds { get; }
}
