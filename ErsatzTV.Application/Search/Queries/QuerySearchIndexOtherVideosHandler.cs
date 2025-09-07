using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexOtherVideosHandler(
        IClient client,
        ISearchIndex searchIndex,
        IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<QuerySearchIndexOtherVideos, OtherVideoCardResultsViewModel>
{
    public async Task<OtherVideoCardResultsViewModel> Handle(
        QuerySearchIndexOtherVideos request,
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
        List<OtherVideoCardViewModel> items = await dbContext.OtherVideoMetadata
            .AsNoTracking()
            .Filter(ovm => ids.Contains(ovm.OtherVideoId))
            .Include(ovm => ovm.OtherVideo)
            .Include(ovm => ovm.Artwork)
            .Include(ovm => ovm.OtherVideo)
            .ThenInclude(ov => ov.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .OrderBy(ovm => ovm.SortTitle)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new OtherVideoCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
