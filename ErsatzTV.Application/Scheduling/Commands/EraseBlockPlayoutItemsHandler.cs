using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class EraseBlockPlayoutItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<EraseBlockPlayoutItems>
{
    public async Task Handle(EraseBlockPlayoutItems request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Playout> maybePlayout = await dbContext.Playouts
            .Include(p => p.Items)
            .Include(p => p.PlayoutHistory)
            .Filter(p => p.ProgramSchedulePlayoutType == ProgramSchedulePlayoutType.Block)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId);

        foreach (Playout playout in maybePlayout)
        {
            // find the earliest item that finishes after "now"
            Option<PlayoutItem> maybeFirstItem = playout.Items
                .Filter(i => i.FinishOffset > DateTimeOffset.Now)
                .OrderBy(i => i.StartOffset)
                .HeadOrNone();

            // delete all history starting with that item
            // importantly, do NOT delete earlier history
            foreach (PlayoutItem item in maybeFirstItem)
            {
                var toRemove = playout.PlayoutHistory.Filter(h => h.When >= item.Start).ToList();
                foreach (PlayoutHistory history in toRemove)
                {
                    playout.PlayoutHistory.Remove(history);
                }
            }

            // save history changes
            await dbContext.SaveChangesAsync(cancellationToken);
            
            // delete all playout items
            await dbContext.Database.ExecuteSqlAsync(
                $"DELETE FROM PlayoutItem WHERE PlayoutId = {playout.Id}",
                cancellationToken);
        }
    }
}
