using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.Search;

public class SearchRerunCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<SearchRerunCollections, List<RerunCollectionViewModel>>
{
    public async Task<List<RerunCollectionViewModel>> Handle(
        SearchRerunCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.RerunCollections
            .AsNoTracking()
            .Where(rc => EF.Functions.Like(rc.Name, $"%{request.Query}%"))
            .OrderBy(rc => rc.Name)
            .Take(10)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
