using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Playouts;

public class UpdateOnDemandCheckpointHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IConfigElementRepository configElementRepository,
    ILogger<UpdateOnDemandCheckpointHandler> logger)
    : IRequestHandler<UpdateOnDemandCheckpoint>
{
    public async Task Handle(UpdateOnDemandCheckpoint request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Playout> maybePlayout = await dbContext.Playouts
            .Include(p => p.Channel)
            .Include(p => p.Items)
            .SelectOneAsync(p => p.Channel.Number, p => p.Channel.Number == request.ChannelNumber);

        foreach (Playout playout in maybePlayout)
        {
            if (playout.Channel.PlayoutMode is not ChannelPlayoutMode.OnDemand)
            {
                return;
            }

            int timeout = await (await configElementRepository.GetValue<int>(ConfigElementKey.FFmpegSegmenterTimeout))
                .IfNoneAsync(60);

            // don't move checkpoint back in time
            DateTimeOffset newCheckpoint = request.Checkpoint - TimeSpan.FromSeconds(timeout);
            if (newCheckpoint > playout.OnDemandCheckpoint)
            {
                playout.OnDemandCheckpoint = newCheckpoint;
            }

            // don't checkpoint before the first item
            // this could happen if you watch a new playout for less time than the segmenter timeout
            if (playout.Items.Count > 0)
            {
                DateTimeOffset minStart = playout.Items.Min(p => p.StartOffset);
                if (playout.OnDemandCheckpoint < minStart)
                {
                    playout.OnDemandCheckpoint = minStart;
                }
            }

            logger.LogDebug(
                "Updating on demand checkpoint for channel {Number} - {Name} to {Checkpoint}",
                playout.Channel.Number,
                playout.Channel.Name,
                playout.OnDemandCheckpoint);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
