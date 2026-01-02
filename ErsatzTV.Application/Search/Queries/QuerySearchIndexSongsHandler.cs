using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ErsatzTV.Core;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class QuerySearchIndexSongsHandler(IClient client, ISearchIndex searchIndex, IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<QuerySearchIndexSongs, SongCardResultsViewModel>
{
    public async Task<SongCardResultsViewModel> Handle(
        QuerySearchIndexSongs request,
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

        var ids = searchResult.Items.Map(i => i.Id).ToHashSet();
        List<SongCardViewModel> items = await dbContext.SongMetadata
            .AsNoTracking()
            .Filter(ovm => ids.Contains(ovm.SongId))
            .Include(ovm => ovm.Song)
            .Include(ovm => ovm.Artwork)
            .Include(sm => sm.Song)
            .ThenInclude(s => s.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .OrderBy(ovm => ovm.SortTitle)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new SongCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
