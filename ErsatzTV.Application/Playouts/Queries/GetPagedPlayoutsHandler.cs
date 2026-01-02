using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Playouts.Mapper;

namespace ErsatzTV.Application.Playouts;

public class GetPagedPlayoutsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPagedPlayouts, PagedPlayoutsViewModel>
{
    public async Task<PagedPlayoutsViewModel> Handle(
        GetPagedPlayouts request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.Playouts.CountAsync(cancellationToken);

        IQueryable<Playout> query = dbContext.Playouts
            .AsNoTracking()
            .Include(p => p.Channel)
            .Include(p => p.ProgramSchedule)
            .Include(p => p.BuildStatus)
            .Filter(p => p.Channel != null);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query = query.Where(p => EF.Functions.Like(p.Channel.Name, $"%{request.Query}%"));
        }

        List<PlayoutNameViewModel> page = await query
            .OrderBy(p => p.Channel.SortNumber)
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedPlayoutsViewModel(count, page);
    }
}
