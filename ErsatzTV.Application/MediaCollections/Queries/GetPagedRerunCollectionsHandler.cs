using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedRerunCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPagedRerunCollections, PagedRerunCollectionsViewModel>
{
    public async Task<PagedRerunCollectionsViewModel> Handle(
        GetPagedRerunCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.RerunCollections.CountAsync(cancellationToken);

        IQueryable<RerunCollection> query = dbContext.RerunCollections.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query = query.Where(rc => EF.Functions.Like(rc.Name, $"%{request.Query}%"));
        }

        List<RerunCollectionViewModel> page = await query
            .OrderBy(rc => rc.Name)
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedRerunCollectionsViewModel(count, page);
    }
}
