using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class ErasePlayoutHistoryHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<ErasePlayoutHistory>
{
    public async Task Handle(ErasePlayoutHistory request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Playout> maybePlayout = await dbContext.Playouts
            .Filter(p => p.ScheduleKind == PlayoutScheduleKind.Block ||
                         p.ScheduleKind == PlayoutScheduleKind.Sequential ||
                         p.ScheduleKind == PlayoutScheduleKind.Scripted ||
                         p.ScheduleKind == PlayoutScheduleKind.Classic)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId, cancellationToken);

        foreach (Playout playout in maybePlayout)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            int nextSeed = new Random().Next();
            playout.Seed = nextSeed;

            // this deletes the owned PlayoutAnchor
            playout.Anchor = null;

            playout.OnDemandCheckpoint = null;

            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.PlayoutItems
                .Where(pi => pi.PlayoutId == playout.Id)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.PlayoutHistory
                .Where(ph => ph.PlayoutId == playout.Id)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.PlayoutProgramScheduleItemAnchors
                .Where(a => a.PlayoutId == playout.Id)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.RerunHistory
                .Where(rh => rh.PlayoutId == playout.Id)
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
