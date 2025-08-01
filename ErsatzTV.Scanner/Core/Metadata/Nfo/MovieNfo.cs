﻿namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public class MovieNfo
{
    public string? Title { get; set; }
    public string? SortTitle { get; set; }
    public string? Outline { get; set; }
    public int Year { get; set; }
    public string? ContentRating { get; set; }
    public Option<DateTime> Premiered { get; set; }

    public string? Plot { get; set; }

    // public string? Tagline { get; set; }
    public List<string> Genres { get; } = [];
    public List<string> Tags { get; } = [];
    public List<string> Studios { get; } = [];
    public List<ActorNfo> Actors { get; } = [];
    public List<string> Writers { get; } = [];
    public List<string> Directors { get; } = [];
    public List<UniqueIdNfo> UniqueIds { get; } = [];
    public List<string> Countries { get; } = [];
}
