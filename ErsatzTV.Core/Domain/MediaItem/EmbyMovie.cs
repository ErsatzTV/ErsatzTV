namespace ErsatzTV.Core.Domain;

public class EmbyMovie : Movie
{
    public string ItemId { get; set; }
    public string Etag { get; set; }
}
