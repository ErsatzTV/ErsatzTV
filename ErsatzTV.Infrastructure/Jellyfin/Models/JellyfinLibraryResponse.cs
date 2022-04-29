namespace ErsatzTV.Infrastructure.Jellyfin.Models;

public class JellyfinLibraryResponse
{
    public string Name { get; set; }
    public string CollectionType { get; set; }
    public string ItemId { get; set; }
    public JellyfinLibraryOptionsResponse LibraryOptions { get; set; }
}
