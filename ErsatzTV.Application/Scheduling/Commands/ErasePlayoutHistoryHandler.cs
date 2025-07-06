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
            .Filter(p => p.ProgramSchedulePlayoutType == ProgramSchedulePlayoutType.Block ||
                         p.ProgramSchedulePlayoutType == ProgramSchedulePlayoutType.Yaml)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId);

        foreach (Playout playout in maybePlayout)
        {
            int nextSeed = new Random().Next();
            playout.Seed = nextSeed;
            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.Database.ExecuteSqlAsync(
                $"DELETE FROM PlayoutItem WHERE PlayoutId = {playout.Id}",
                cancellationToken);

            await dbContext.Database.ExecuteSqlAsync(
                $"DELETE FROM PlayoutHistory WHERE PlayoutId = {playout.Id}",
                cancellationToken);
        }
    }
}
