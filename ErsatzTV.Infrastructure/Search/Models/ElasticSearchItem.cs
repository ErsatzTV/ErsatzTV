using System.Text.Json.Serialization;

namespace ErsatzTV.Infrastructure.Search.Models;

public class ElasticSearchItem
{
    [JsonPropertyName(SearchIndex.IdField)]
    public int Id { get; set; }
    
    [JsonPropertyName(SearchIndex.TypeField)]
    public string Type { get; set; }
    
    [JsonPropertyName(SearchIndex.SortTitleField)]
    public string SortTitle { get; set; }
}
