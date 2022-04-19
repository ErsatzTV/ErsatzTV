namespace ErsatzTV.Core.Search;

public record SearchResult(List<SearchItem> Items, int TotalCount)
{
    public Option<SearchPageMap> PageMap { get; set; }
}
