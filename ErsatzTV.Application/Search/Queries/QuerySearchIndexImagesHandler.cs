using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class QuerySearchIndexImagesHandler(
    IClient client,
    ISearchIndex searchIndex,
    IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<QuerySearchIndexImages, ImageCardResultsViewModel>
{
    public async Task<ImageCardResultsViewModel> Handle(
        QuerySearchIndexImages request,
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
        List<ImageCardViewModel> items = await dbContext.ImageMetadata
            .AsNoTracking()
            .Filter(im => ids.Contains(im.ImageId))
            .Include(im => im.Image)
            .Include(im => im.Artwork)
            .Include(im => im.Image)
            .ThenInclude(s => s.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .OrderBy(im => im.SortTitle)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new ImageCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
