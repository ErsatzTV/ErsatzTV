using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedTraktListsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPagedTraktLists, PagedTraktListsViewModel>
{
    public async Task<PagedTraktListsViewModel> Handle(
        GetPagedTraktLists request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.TraktLists.CountAsync(cancellationToken);
        List<TraktListViewModel> page = await dbContext.TraktLists
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .Include(l => l.Items)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedTraktListsViewModel(count, page);
    }
}
