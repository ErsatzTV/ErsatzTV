using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class GetAllBlockPlayoutsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllBlockPlayouts, List<PlayoutNameViewModel>>
{
    public async Task<List<PlayoutNameViewModel>> Handle(
        GetAllBlockPlayouts request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Playout> playouts = await dbContext.Playouts
            .AsNoTracking()
            .Include(p => p.Channel)
            .Include(p => p.ProgramSchedule)
            .Include(p => p.BuildStatus)
            .Where(p => p.Channel != null && p.ScheduleKind == PlayoutScheduleKind.Block)
            .ToListAsync(cancellationToken);

        return playouts.Map(Mapper.ProjectToViewModel).ToList();
    }
}
