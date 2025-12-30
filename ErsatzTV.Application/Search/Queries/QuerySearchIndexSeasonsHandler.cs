using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ErsatzTV.Core;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexSeasonsHandler(
        IClient client,
        ISearchIndex searchIndex,
        IDbContextFactory<TvContext> dbContextFactory)
    : QuerySearchIndexHandlerBase, IRequestHandler<QuerySearchIndexSeasons, TelevisionSeasonCardResultsViewModel>
{
    public async Task<TelevisionSeasonCardResultsViewModel> Handle(
        QuerySearchIndexSeasons request,
        CancellationToken cancellationToken)
    {
        int pageSize = PaginationOptions.NormalizePageSize(request.PageSize);

        SearchResult searchResult = await searchIndex.Search(
            client,
            request.Query,
            string.Empty,
            (request.PageNumber - 1) * pageSize,
            pageSize,
            cancellationToken);

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<JellyfinMediaSource> maybeJellyfin = await GetJellyfin(dbContext, cancellationToken);
        Option<EmbyMediaSource> maybeEmby = await GetEmby(dbContext, cancellationToken);

        var ids = searchResult.Items.Map(i => i.Id).ToHashSet();
        List<TelevisionSeasonCardViewModel> items = await dbContext.SeasonMetadata
            .AsNoTracking()
            .Filter(s => ids.Contains(s.SeasonId))
            .Include(s => s.Season.Show)
            .ThenInclude(s => s.ShowMetadata)
            .Include(sm => sm.Artwork)
            .ToListAsync(cancellationToken)
            .Map(list => list
                .OrderBy(s => s.Season.Show.ShowMetadata.HeadOrNone().Match(sm => sm.SortTitle, () => string.Empty))
                .ThenBy(s => s.Season.SeasonNumber)
                .ToList())
            .Map(list => list.Map(s => ProjectToViewModel(s, maybeJellyfin, maybeEmby)).ToList());

        return new TelevisionSeasonCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
