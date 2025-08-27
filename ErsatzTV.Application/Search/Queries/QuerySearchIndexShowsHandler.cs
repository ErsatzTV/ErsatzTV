using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexShowsHandler(
        IClient client,
        ISearchIndex searchIndex,
        IDbContextFactory<TvContext> dbContextFactory)
    : QuerySearchIndexHandlerBase, IRequestHandler<QuerySearchIndexShows, TelevisionShowCardResultsViewModel>
{
    public async Task<TelevisionShowCardResultsViewModel> Handle(
        QuerySearchIndexShows request,
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

        Option<JellyfinMediaSource> maybeJellyfin = await GetJellyfin(dbContext, cancellationToken);
        Option<EmbyMediaSource> maybeEmby = await GetEmby(dbContext, cancellationToken);

        var ids = searchResult.Items.Map(i => i.Id).ToHashSet();
        List<TelevisionShowCardViewModel> items = await dbContext.ShowMetadata
            .AsNoTracking()
            .Filter(sm => ids.Contains(sm.ShowId))
            .Include(sm => sm.Artwork)
            .Include(sm => sm.Show)
            .OrderBy(sm => sm.SortTitle)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(s => ProjectToViewModel(s, maybeJellyfin, maybeEmby)).ToList());

        return new TelevisionShowCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
