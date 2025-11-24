using System.IO.Abstractions;
using System.Threading.Channels;
using ErsatzTV.Application.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Notifications;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class DeletePlayoutHandler(
    ChannelWriter<IBackgroundServiceRequest> workerChannel,
    IDbContextFactory<TvContext> dbContextFactory,
    IFileSystem fileSystem,
    IMediator mediator)
    : IRequestHandler<DeletePlayout, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(DeletePlayout request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Playout> maybePlayout = await dbContext.Playouts
            .Include(p => p.Channel)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId, cancellationToken);

        foreach (Playout playout in maybePlayout)
        {
            dbContext.Playouts.Remove(playout);
            await dbContext.SaveChangesAsync(cancellationToken);

            // delete channel data from channel guide cache
            string cacheFile = Path.Combine(FileSystemLayout.ChannelGuideCacheFolder, $"{playout.Channel.Number}.xml");
            if (fileSystem.File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }

            // refresh channel list to remove channel that has no playout
            await workerChannel.WriteAsync(new RefreshChannelList(), cancellationToken);

            await mediator.Publish(new PlayoutUpdatedNotification(playout.Id, false), cancellationToken);
        }

        return maybePlayout
            .Map(_ => Unit.Default)
            .ToEither(BaseError.New($"Playout {request.PlayoutId} does not exist."));
    }
}
