namespace ErsatzTV.Core.Domain;

public class Tag
{
    public static readonly string PlexNetworkTypeId = "319";

    public int Id { get; set; }
    public string Name { get; set; }
    public string ExternalCollectionId { get; set; }
    public string ExternalTypeId { get; set; }
}
