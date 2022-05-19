namespace ErsatzTV.Core.Domain;

public class PlexEpisode : Episode
{
    public string Key { get; set; }
    public string Etag { get; set; }
}
