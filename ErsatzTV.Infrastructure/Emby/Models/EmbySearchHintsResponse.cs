namespace ErsatzTV.Infrastructure.Emby.Models;

public class EmbySearchHintsResponse
{
    public List<EmbySearchHintResponse> SearchHints { get; set; } = [];
    public int TotalRecordCount { get; set; }
}

public class EmbySearchHintResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string MatchedTerm { get; set; }
    public int? IndexNumber { get; set; }
    public int? ProductionYear { get; set; }
    public string Overview { get; set; }
    public EmbyImageTagsResponse ImageTags { get; set; } = new();
    public List<string> BackdropImageTags { get; set; } = [];
}