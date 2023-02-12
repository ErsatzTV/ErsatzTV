namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public class EpisodeNfo
{
    public EpisodeNfo()
    {
        Genres = new List<string>();
        Tags = new List<string>();
        Actors = new List<ActorNfo>();
        Writers = new List<string>();
        Directors = new List<string>();
        UniqueIds = new List<UniqueIdNfo>();
    }

    public string? ShowTitle { get; set; }
    public string? Title { get; set; }
    public int Episode { get; set; }
    public int Season { get; set; }
    public string? ContentRating { get; set; }
    public Option<DateTime> Aired { get; set; }
    public string? Plot { get; set; }
    public List<string> Genres { get; }
    public List<string> Tags { get; }
    public List<ActorNfo> Actors { get; }
    public List<string> Writers { get; }
    public List<string> Directors { get; }
    public List<UniqueIdNfo> UniqueIds { get; }
}
