namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexResourceConnection
{
    public string Protocol { get; set; }
    public string Address { get; set; }
    public int Port { get; set; }
    public string Uri { get; set; }
    public bool Local { get; set; }
}