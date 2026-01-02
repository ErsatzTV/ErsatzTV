using ErsatzTV.Core.Domain;
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

        IQueryable<SmartCollection> query = dbContext.SmartCollections.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query = query.Where(sc => EF.Functions.Like(sc.Name, $"%{request.Query}%"));
        }

        List<SmartCollectionViewModel> page = await query
            .OrderBy(s => s.Name)
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedSmartCollectionsViewModel(count, page);
    }
}
