using System.Threading.Channels;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class ResetAllPlayoutsHandler(
    IEntityLocker locker,
    ChannelWriter<IBackgroundServiceRequest> channel,
    IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<ResetAllPlayouts>
{
    public async Task Handle(ResetAllPlayouts request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        foreach (Playout playout in await dbContext.Playouts.ToListAsync(cancellationToken))
        {
            switch (playout.ProgramSchedulePlayoutType)
            {
                case ProgramSchedulePlayoutType.Flood:
                case ProgramSchedulePlayoutType.Block:
                case ProgramSchedulePlayoutType.Yaml:
                    if (!locker.IsPlayoutLocked(playout.Id))
                    {
                        await channel.WriteAsync(new BuildPlayout(playout.Id, PlayoutBuildMode.Reset), cancellationToken);
                    }
                    break;
                case ProgramSchedulePlayoutType.ExternalJson:
                case ProgramSchedulePlayoutType.None:
                default:
                    // external json cannot be reset
                    continue;
            }
        }
    }
}
