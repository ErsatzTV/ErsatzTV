using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.Search;

public class SearchSmartCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<SearchSmartCollections, List<SmartCollectionViewModel>>
{
    public async Task<List<SmartCollectionViewModel>> Handle(
        SearchSmartCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.SmartCollections
            .AsNoTracking()
            .Where(c => EF.Functions.Like(
                EF.Functions.Collate(c.Name, TvContext.CaseInsensitiveCollation),
                $"%{request.Query}%"))
            .OrderBy(c => EF.Functions.Collate(c.Name, TvContext.CaseInsensitiveCollation))
            .Take(10)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
