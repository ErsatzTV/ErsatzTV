namespace ErsatzTV.Core.Domain;

public class ShowMetadata : Metadata
{
    public string ContentRating { get; set; }
    public string Outline { get; set; }
    public string Plot { get; set; }
    public string Tagline { get; set; }
    public int ShowId { get; set; }
    public Show Show { get; set; }
}
