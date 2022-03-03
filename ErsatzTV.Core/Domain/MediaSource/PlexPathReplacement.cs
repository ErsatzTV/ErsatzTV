namespace ErsatzTV.Core.Domain;

public class PlexPathReplacement
{
    public int Id { get; set; }
    public string PlexPath { get; set; }
    public string LocalPath { get; set; }
    public int PlexMediaSourceId { get; set; }
    public PlexMediaSource PlexMediaSource { get; set; }
}