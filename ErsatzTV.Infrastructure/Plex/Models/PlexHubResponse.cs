namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexMediaContainerHubContent<T>
{
    public List<T> Hub { get; set; } = [];
}

public class PlexHubResponse
{
    public string HubIdentifier { get; set; }
    public string HubKey { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public List<PlexMetadataResponse> Metadata { get; set; } = [];
}