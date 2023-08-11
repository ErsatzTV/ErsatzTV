using System.Text.Json.Serialization;

namespace ErsatzTV.Infrastructure.Search.Models;

public class MinimalElasticSearchItem
{
    [JsonPropertyName(LuceneSearchIndex.IdField)]
    public int Id { get; set; }

    [JsonPropertyName(LuceneSearchIndex.TypeField)]
    public string Type { get; set; }

    [JsonPropertyName(LuceneSearchIndex.SortTitleField)]
    public string SortTitle { get; set; }

    [JsonPropertyName(LuceneSearchIndex.JumpLetterField)]
    public string JumpLetter { get; set; }
}
