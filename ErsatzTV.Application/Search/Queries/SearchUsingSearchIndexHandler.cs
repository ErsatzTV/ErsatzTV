using System.Collections.Immutable;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Search;

namespace ErsatzTV.Application.Search;

public abstract class SearchUsingSearchIndexHandler(ISearchIndex searchIndex)
{
    private const int PageSize = 10;

    protected async Task<ImmutableHashSet<int>> Search(string type, string query, CancellationToken cancellationToken)
    {
        var searchResult = await searchIndex.Search(
            $"type:{type} AND *{query.Replace(" ", @"\ ")}*",
            string.Empty,
            0,
            PageSize,
            [LuceneSearchIndex.TitleAndYearSearchField],
            cancellationToken);

        return searchResult.Items.Select(i => i.Id).ToImmutableHashSet();
    }
}
