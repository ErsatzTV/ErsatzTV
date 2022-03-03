using Newtonsoft.Json;

namespace ErsatzTV.Infrastructure.Trakt.Models;

public class TraktListResponse
{
    public string Name { get; set; }
    public string Description { get; set; }
    [JsonProperty("item_count")]
    public int ItemCount { get; set; }
    public TraktListIds Ids { get; set; }
    public TraktUser User { get; set; }
}