namespace ErsatzTV.Core.Domain;

public class PlexConnection
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public string Uri { get; set; }
    public int PlexMediaSourceId { get; set; }
    public PlexMediaSource PlexMediaSource { get; set; }
}
