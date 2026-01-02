using ErsatzTV.Core;
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
        int pageSize = PaginationOptions.NormalizePageSize(request.PageSize);

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.SmartCollections.CountAsync(cancellationToken);

        IQueryable<SmartCollection> query = dbContext.SmartCollections.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query = query.Where(s => EF.Functions.Like(
                EF.Functions.Collate(s.Name, TvContext.CaseInsensitiveCollation),
                $"%{request.Query}%"));
        }

        List<SmartCollectionViewModel> page = await query
            .OrderBy(s => EF.Functions.Collate(s.Name, TvContext.CaseInsensitiveCollation))
            .Skip(request.PageNum * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedSmartCollectionsViewModel(count, page);
    }
}
