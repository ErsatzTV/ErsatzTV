using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class QuerySearchIndexMoviesHandler(
    IClient client,
    ISearchIndex searchIndex,
    IDbContextFactory<TvContext> dbContextFactory)
    : QuerySearchIndexHandlerBase, IRequestHandler<QuerySearchIndexMovies, MovieCardResultsViewModel>
{
    public async Task<MovieCardResultsViewModel> Handle(
        QuerySearchIndexMovies request,
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
        List<MovieCardViewModel> items = await dbContext.MovieMetadata
            .AsNoTracking()
            .Filter(mm => ids.Contains(mm.MovieId))
            .Include(mm => mm.Artwork)
            .Include(mm => mm.Movie)
            .ThenInclude(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .OrderBy(mm => mm.SortTitle)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(m => ProjectToViewModel(m, maybeJellyfin, maybeEmby)).ToList());

        return new MovieCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
