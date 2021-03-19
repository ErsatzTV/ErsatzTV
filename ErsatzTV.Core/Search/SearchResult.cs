using System.Collections.Generic;

namespace ErsatzTV.Core.Search
{
    public record SearchResult(List<SearchItem> Items, int TotalCount);
}
