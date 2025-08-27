using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class QuerySearchIndexRemoteStreamsHandler(
    IClient client,
    ISearchIndex searchIndex,
    IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<QuerySearchIndexRemoteStreams, RemoteStreamCardResultsViewModel>
{
    public async Task<RemoteStreamCardResultsViewModel> Handle(
        QuerySearchIndexRemoteStreams request,
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
        List<RemoteStreamCardViewModel> items = await dbContext.RemoteStreamMetadata
            .AsNoTracking()
            .Filter(im => ids.Contains(im.RemoteStreamId))
            .Include(im => im.RemoteStream)
            .Include(im => im.Artwork)
            .Include(im => im.RemoteStream)
            .ThenInclude(s => s.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .OrderBy(im => im.SortTitle)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new RemoteStreamCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
