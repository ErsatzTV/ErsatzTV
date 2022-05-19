namespace ErsatzTV.Infrastructure.Jellyfin.Models;

public class JellyfinLibraryItemResponse
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string Etag { get; set; }
    public string Path { get; set; }
    public string OfficialRating { get; set; }
    public DateTimeOffset DateCreated { get; set; }
    public long RunTimeTicks { get; set; }
    public List<string> Genres { get; set; }
    public List<string> Tags { get; set; }
    public int ProductionYear { get; set; }
    public JellyfinProviderIdsResponse ProviderIds { get; set; }
    public string PremiereDate { get; set; }
    public List<JellyfinMediaStreamResponse> MediaStreams { get; set; }
    public string LocationType { get; set; }
    public string Overview { get; set; }
    public List<string> Taglines { get; set; }
    public List<JellyfinStudioResponse> Studios { get; set; }
    public List<JellyfinPersonResponse> People { get; set; }
    public JellyfinImageTagsResponse ImageTags { get; set; }
    public List<string> BackdropImageTags { get; set; }
    public int? IndexNumber { get; set; }
    public string Type { get; set; }
}
