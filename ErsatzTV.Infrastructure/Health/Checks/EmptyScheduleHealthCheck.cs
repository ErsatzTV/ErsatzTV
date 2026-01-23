using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class EmptyScheduleHealthCheck(IDbContextFactory<TvContext> dbContextFactory) : BaseHealthCheck, IEmptyScheduleHealthCheck
{
    public override string Title => "Empty Schedule";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<int> scheduleIdsWithPlayouts = await dbContext.ProgramSchedules
            .TagWithCallSite()
            .AsNoTracking()
            .Where(s => s.Playouts.Count > 0 && s.Items.Count == 0)
            .Select(ps => ps.Id)
            .ToListAsync(cancellationToken);

        List<int> alternateScheduleIds = await dbContext.ProgramScheduleAlternates
            .TagWithCallSite()
            .AsNoTracking()
            .Where(s => s.ProgramSchedule.Items.Count == 0)
            .Select(s => s.ProgramScheduleId)
            .ToListAsync(cancellationToken);

        var ids = scheduleIdsWithPlayouts.Union(alternateScheduleIds).ToHashSet();

        if (ids.Count > 0)
        {
            List<string> names = await dbContext.ProgramSchedules
                .TagWithCallSite()
                .AsNoTracking()
                .Where(s => ids.Contains(s.Id))
                .Select(s => s.Name)
                .ToListAsync(cancellationToken);

            return WarningResult(
                $"There are {names.Count} empty schedules in use, which are NOT supported and WILL cause errors, including: {string.Join(", ", names.OrderBy(identity))}",
                $"There are {names.Count} empty schedules in use, which are NOT supported and WILL cause errors");
        }

        return OkResult();
    }
}
