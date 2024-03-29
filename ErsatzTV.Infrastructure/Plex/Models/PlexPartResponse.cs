namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexPartResponse
{
    public int Id { get; set; }
    public string Key { get; set; }
    public int Duration { get; set; }
    public string File { get; set; }
    public long Size { get; set; }
    public string Container { get; set; }
    public string VideoProfile { get; set; }
    public string AudioProfile { get; set; }
}
