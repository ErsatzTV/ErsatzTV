using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedMultiCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPagedMultiCollections, PagedMultiCollectionsViewModel>
{
    public async Task<PagedMultiCollectionsViewModel> Handle(
        GetPagedMultiCollections request,
        CancellationToken cancellationToken)
    {
        int pageSize = PaginationOptions.NormalizePageSize(request.PageSize);

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.MultiCollections.CountAsync(cancellationToken);

        IQueryable<MultiCollection> query = dbContext.MultiCollections.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query = query.Where(s => EF.Functions.Like(
                EF.Functions.Collate(s.Name, TvContext.CaseInsensitiveCollation),
                $"%{request.Query}%"));
        }

        List<MultiCollectionViewModel> page = await query
            .OrderBy(f => EF.Functions.Collate(f.Name, TvContext.CaseInsensitiveCollation))
            .Skip(request.PageNum * pageSize)
            .Take(pageSize)
            .Include(mc => mc.MultiCollectionItems)
            .ThenInclude(i => i.Collection)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedMultiCollectionsViewModel(count, page);
    }
}
