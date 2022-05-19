namespace ErsatzTV.Core.Domain;

public class OtherVideoMetadata : Metadata
{
    public string ContentRating { get; set; }
    public string Outline { get; set; }
    public string Plot { get; set; }
    public string Tagline { get; set; }
    public int OtherVideoId { get; set; }
    public OtherVideo OtherVideo { get; set; }
    public List<Director> Directors { get; set; }
    public List<Writer> Writers { get; set; }
}
