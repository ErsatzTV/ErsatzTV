using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class GetBlockPlayoutHistoryHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetBlockPlayoutHistory, PagedPlayoutHistoryViewModel>
{
    public async Task<PagedPlayoutHistoryViewModel> Handle(
        GetBlockPlayoutHistory request,
        CancellationToken cancellationToken)
    {
        int pageSize = PaginationOptions.NormalizePageSize(request.PageSize);

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<PlayoutHistory> query = dbContext.PlayoutHistory
            .AsNoTracking()
            .Where(ph => ph.PlayoutId == request.PlayoutId && ph.BlockId == request.BlockId);

        int totalCount = await query.CountAsync(cancellationToken);

        List<PlayoutHistory> allHistory = await query
            .OrderBy(ph => ph.Id)
            .Skip(request.PageNum * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedPlayoutHistoryViewModel(totalCount, allHistory.Map(Mapper.ProjectToViewModel).ToList());
    }
}
