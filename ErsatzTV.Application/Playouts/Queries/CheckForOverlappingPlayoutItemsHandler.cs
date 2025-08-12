using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Playouts;

public class CheckForOverlappingPlayoutItemsHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    ILogger<CheckForOverlappingPlayoutItemsHandler> logger)
    : IRequestHandler<CheckForOverlappingPlayoutItems>
{
    public async Task Handle(CheckForOverlappingPlayoutItems request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        bool hasConflict = await dbContext.PlayoutItems
            .Where(pi => pi.PlayoutId == request.PlayoutId)
            .AnyAsync(a => dbContext.PlayoutItems
                    .Any(b =>
                        a.Id < b.Id &&
                        a.Start < b.Finish &&
                        a.Finish > b.Start),
                cancellationToken);

        if (hasConflict)
        {
            var maybeChannel = await dbContext.Channels
                .AsNoTracking()
                .Where(c => c.Playouts.Any(p => p.Id == request.PlayoutId))
                .FirstOrDefaultAsync(cancellationToken)
                .Map(Optional);

            foreach (var channel in maybeChannel)
            {
                logger.LogWarning(
                    "Playout for channel {ChannelName} has overlapping playout items; this is a bug.",
                    channel.Name);
            }
        }
    }
}