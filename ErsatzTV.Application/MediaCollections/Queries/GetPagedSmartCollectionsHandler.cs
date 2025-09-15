using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedSmartCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPagedSmartCollections, PagedSmartCollectionsViewModel>
{
    public async Task<PagedSmartCollectionsViewModel> Handle(
        GetPagedSmartCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.SmartCollections.CountAsync(cancellationToken);
        List<SmartCollectionViewModel> page = await dbContext.SmartCollections
            .AsNoTracking()
            .OrderBy(f => EF.Functions.Collate(f.Name, TvContext.CaseInsensitiveCollation))
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedSmartCollectionsViewModel(count, page);
    }
}
