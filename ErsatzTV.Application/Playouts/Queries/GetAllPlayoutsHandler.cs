using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class GetAllPlayoutsHandler : IRequestHandler<GetAllPlayouts, List<PlayoutNameViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetAllPlayoutsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<PlayoutNameViewModel>> Handle(
        GetAllPlayouts request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Playouts
            .AsNoTracking()
            .Include(p => p.ProgramSchedule)
            .Filter(p => p.Channel != null)
            .Map(
                p => new PlayoutNameViewModel(
                    p.Id,
                    p.ProgramSchedulePlayoutType,
                    p.Channel.Name,
                    p.Channel.Number,
                    p.ProgramScheduleId == null ? string.Empty : p.ProgramSchedule.Name,
                    p.ExternalJsonFile,
                    Optional(p.DailyRebuildTime)))
            .ToListAsync(cancellationToken);
    }
}
