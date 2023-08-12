namespace ErsatzTV.Core.Search;

public record SearchResult(List<SearchItem> Items, int TotalCount)
{
    public SearchPageMap PageMap { get; set; }
}
