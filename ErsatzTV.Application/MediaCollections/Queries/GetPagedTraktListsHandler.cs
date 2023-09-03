using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedTraktListsHandler : IRequestHandler<GetPagedTraktLists, PagedTraktListsViewModel>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetPagedTraktListsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<PagedTraktListsViewModel> Handle(
        GetPagedTraktLists request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.TraktLists.CountAsync(cancellationToken);
        List<TraktListViewModel> page = await dbContext.TraktLists
            .AsNoTracking()
            .OrderBy(f => EF.Functions.Collate(f.Name, TvContext.CaseInsensitiveCollation))
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .Include(l => l.Items)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedTraktListsViewModel(count, page);
    }
}
