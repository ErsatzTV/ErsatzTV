using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class EraseBlockPlayoutHistoryHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<EraseBlockPlayoutHistory>
{
    public async Task Handle(EraseBlockPlayoutHistory request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Playout> maybePlayout = await dbContext.Playouts
            .Include(p => p.Items)
            .Include(p => p.PlayoutHistory)
            .Filter(p => p.ProgramSchedulePlayoutType == ProgramSchedulePlayoutType.Block)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId);

        foreach (Playout playout in maybePlayout)
        {
            playout.Items.Clear();
            playout.PlayoutHistory.Clear();

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
