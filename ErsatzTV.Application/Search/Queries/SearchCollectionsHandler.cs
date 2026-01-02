using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.Search;

public class SearchCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<SearchCollections, List<MediaCollectionViewModel>>
{
    public async Task<List<MediaCollectionViewModel>> Handle(
        SearchCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Collections
            .AsNoTracking()
            .Where(c => EF.Functions.Like(c.Name, $"%{request.Query}%"))
            .OrderBy(c => c.Name)
            .Take(10)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
