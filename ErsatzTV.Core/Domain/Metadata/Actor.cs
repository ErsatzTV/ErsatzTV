namespace ErsatzTV.Core.Domain;

public class Actor
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public int? Order { get; set; }
    public int? ArtworkId { get; set; }
    public Artwork Artwork { get; set; }
}