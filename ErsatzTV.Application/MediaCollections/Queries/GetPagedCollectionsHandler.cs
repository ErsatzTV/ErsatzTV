using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPagedCollections, PagedMediaCollectionsViewModel>
{
    public async Task<PagedMediaCollectionsViewModel> Handle(
        GetPagedCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.Collections.CountAsync(cancellationToken);

        IQueryable<Collection> query = dbContext.Collections.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{request.Query}%"));
        }

        List<MediaCollectionViewModel> page = await query
            .OrderBy(c => c.Name)
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedMediaCollectionsViewModel(count, page);
    }
}
