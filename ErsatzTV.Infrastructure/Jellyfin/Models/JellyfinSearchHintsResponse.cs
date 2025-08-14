namespace ErsatzTV.Infrastructure.Jellyfin.Models;

public class JellyfinSearchHintsResponse
{
    public List<JellyfinSearchHintResponse> SearchHints { get; set; } = [];
    public int TotalRecordCount { get; set; }
}

public class JellyfinSearchHintResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string MatchedTerm { get; set; }
    public int? IndexNumber { get; set; }
    public int? ProductionYear { get; set; }
    public string Overview { get; set; }
    public JellyfinImageTagsResponse ImageTags { get; set; } = new();
    public List<string> BackdropImageTags { get; set; } = [];
}