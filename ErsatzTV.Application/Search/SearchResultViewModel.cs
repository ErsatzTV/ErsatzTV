namespace ErsatzTV.Application.Search;

public class SearchResultViewModel<T>
{
    public int TotalCount { get; set; }
    public List<T> Items { get; set; }
}