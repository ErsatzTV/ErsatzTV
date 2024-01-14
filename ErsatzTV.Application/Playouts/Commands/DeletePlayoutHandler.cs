using System.Threading.Channels;
using ErsatzTV.Application.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class DeletePlayoutHandler : IRequestHandler<DeletePlayout, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

    public DeletePlayoutHandler(
        ChannelWriter<IBackgroundServiceRequest> workerChannel,
        IDbContextFactory<TvContext> dbContextFactory,
        ILocalFileSystem localFileSystem)
    {
        _workerChannel = workerChannel;
        _dbContextFactory = dbContextFactory;
        _localFileSystem = localFileSystem;
    }

    public async Task<Either<BaseError, Unit>> Handle(DeletePlayout request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Playout> maybePlayout = await dbContext.Playouts
            .Include(p => p.Channel)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId);

        foreach (Playout playout in maybePlayout)
        {
            dbContext.Playouts.Remove(playout);
            await dbContext.SaveChangesAsync(cancellationToken);

            // delete channel data from channel guide cache
            string cacheFile = Path.Combine(FileSystemLayout.ChannelGuideCacheFolder, $"{playout.Channel.Number}.xml");
            if (_localFileSystem.FileExists(cacheFile))
            {
                File.Delete(cacheFile);
            }

            // refresh channel list to remove channel that has no playout
            await _workerChannel.WriteAsync(new RefreshChannelList(), cancellationToken);
        }

        return maybePlayout
            .Map(_ => Unit.Default)
            .ToEither(BaseError.New($"Playout {request.PlayoutId} does not exist."));
    }
}
