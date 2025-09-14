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
        List<RerunCollectionViewModel> page = await dbContext.RerunCollections
            .AsNoTracking()
            .OrderBy(f => EF.Functions.Collate(f.Name, TvContext.CaseInsensitiveCollation))
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedRerunCollectionsViewModel(count, page);
    }
}
