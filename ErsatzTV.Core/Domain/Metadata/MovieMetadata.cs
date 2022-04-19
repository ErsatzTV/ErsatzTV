namespace ErsatzTV.Core.Domain;

public class MovieMetadata : Metadata
{
    public string ContentRating { get; set; }
    public string Outline { get; set; }
    public string Plot { get; set; }
    public string Tagline { get; set; }
    public int MovieId { get; set; }
    public Movie Movie { get; set; }
    public List<Director> Directors { get; set; }
    public List<Writer> Writers { get; set; }
}
