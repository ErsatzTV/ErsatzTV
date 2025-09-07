using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class QuerySearchIndexArtistsHandler(
    IClient client,
    ISearchIndex searchIndex,
    IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<QuerySearchIndexArtists, ArtistCardResultsViewModel>
{
    public async Task<ArtistCardResultsViewModel> Handle(
        QuerySearchIndexArtists request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = await searchIndex.Search(
            client,
            request.Query,
            string.Empty,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var ids = searchResult.Items.Map(i => i.Id).ToHashSet();
        List<ArtistCardViewModel> items = await dbContext.ArtistMetadata
            .AsNoTracking()
            .Filter(am => ids.Contains(am.ArtistId))
            .Include(am => am.Artist)
            .Include(am => am.Artwork)
            .OrderBy(am => am.SortTitle)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new ArtistCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
