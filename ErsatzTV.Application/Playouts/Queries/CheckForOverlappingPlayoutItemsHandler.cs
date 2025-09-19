using ErsatzTV.Core.Domain;
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

        IAsyncEnumerable<PlayoutItemDto> items = dbContext.PlayoutItems
            .AsNoTracking()
            .Where(pi => pi.PlayoutId == request.PlayoutId)
            .OrderBy(pi => pi.Start)
            .Select(pi => new PlayoutItemDto(pi.Start, pi.Finish))
            .AsAsyncEnumerable();

        var hasConflict = false;
        DateTime? maxFinish = null;
        var isFirstItem = true;

        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            if (isFirstItem)
            {
                maxFinish = item.Finish;
                isFirstItem = false;
                continue;
            }

            if (item.Start < maxFinish)
            {
                hasConflict = true;
                break;
            }

            if (item.Finish > maxFinish)
            {
                maxFinish = item.Finish;
            }
        }

        if (hasConflict)
        {
            Option<Channel> maybeChannel = await dbContext.Channels
                .AsNoTracking()
                .Where(c => c.Playouts.Any(p => p.Id == request.PlayoutId))
                .FirstOrDefaultAsync(cancellationToken)
                .Map(Optional);

            foreach (Channel channel in maybeChannel)
            {
                logger.LogWarning(
                    "Playout for channel {ChannelName} has overlapping playout items; this may be a bug.",
                    channel.Name);
            }
        }
    }

    private sealed record PlayoutItemDto(DateTime Start, DateTime Finish);
}
