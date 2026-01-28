using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class ErasePlayoutItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<ErasePlayoutItems>
{
    public async Task Handle(ErasePlayoutItems request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Playout> maybePlayout = await dbContext.Playouts
            .TagWithCallSite()
            .AsNoTracking()
            .Filter(p => p.ScheduleKind == PlayoutScheduleKind.Block ||
                         p.ScheduleKind == PlayoutScheduleKind.Sequential ||
                         p.ScheduleKind == PlayoutScheduleKind.Scripted)
            .SingleOrDefaultAsync(p => p.Id == request.PlayoutId, cancellationToken);

        foreach (Playout playout in maybePlayout)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            // find the earliest item that finishes after "now"
            Option<PlayoutItem> maybeFirstItem = await dbContext.PlayoutItems
                .TagWithCallSite()
                .AsNoTracking()
                .Where(pi => pi.PlayoutId == playout.Id)
                .Where(pi => pi.Finish > DateTime.UtcNow)
                .OrderBy(i => i.Start)
                .FirstOrDefaultAsync(cancellationToken);

            foreach (PlayoutItem firstItem in maybeFirstItem)
            {
                // delete all history starting with that item
                // importantly, do NOT delete earlier history
                await dbContext.PlayoutHistory
                    .Where(ph => ph.PlayoutId == playout.Id)
                    .Where(ph => ph.When >= firstItem.Start)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await dbContext.PlayoutItems
                .Where(pi => pi.PlayoutId == playout.Id)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.PlayoutGaps
                .Where(pg => pg.PlayoutId == playout.Id)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.PlayoutBuildStatus
                .Where(pb => pb.PlayoutId == playout.Id)
                .ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
    }
}
