namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexLibraryResponse
{
    public string Key { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public string Agent { get; set; }
    public int Hidden { get; set; }
    public string Uuid { get; set; }
}
