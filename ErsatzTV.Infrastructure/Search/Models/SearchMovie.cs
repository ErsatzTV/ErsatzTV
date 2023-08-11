using System.Text.Json.Serialization;

namespace ErsatzTV.Infrastructure.Search.Models;

public class SearchMovie : BaseSearchItem
{
    [JsonPropertyName("type")]
    public override string Type => SearchIndex.MovieType;
}
