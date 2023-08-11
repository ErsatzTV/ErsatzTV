using System.Text.Json.Serialization;

namespace ErsatzTV.Infrastructure.Search.Models;

public class SearchMovie : BaseSearchItem
{
    [JsonPropertyName(SearchIndex.TypeField)]
    public override string Type => SearchIndex.MovieType;
}
