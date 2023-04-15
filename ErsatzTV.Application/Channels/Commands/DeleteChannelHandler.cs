using System.Threading;
using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Channels;

public class DeleteChannelHandler : IRequestHandler<DeleteChannel, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;

    public DeleteChannelHandler(
        ChannelWriter<IBackgroundServiceRequest> workerChannel,
        IDbContextFactory<TvContext> dbContextFactory,
        ILocalFileSystem localFileSystem)
    {
        _workerChannel = workerChannel;
        _dbContextFactory = dbContextFactory;
        _localFileSystem = localFileSystem;
    }

    public async Task<Either<BaseError, Unit>> Handle(DeleteChannel request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Core.Domain.Channel> validation = await ChannelMustExist(dbContext, request);

        return await validation.Apply(c => DoDeletion(dbContext, c, cancellationToken));
    }

    private async Task<Unit> DoDeletion(TvContext dbContext, Core.Domain.Channel channel, CancellationToken cancellationToken)
    {
        dbContext.Channels.Remove(channel);
        await dbContext.SaveChangesAsync();

        // delete channel data from channel guide cache
        string cacheFile = Path.Combine(FileSystemLayout.ChannelGuideCacheFolder, $"{channel.Number}.xml");
        if (_localFileSystem.FileExists(cacheFile))
        {
            File.Delete(cacheFile);
        }

        // refresh channel list to remove channel that has no playout
        await _workerChannel.WriteAsync(new RefreshChannelList(), cancellationToken);

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Core.Domain.Channel>> ChannelMustExist(TvContext dbContext, DeleteChannel deleteChannel)
    {
        Option<Core.Domain.Channel> maybeChannel = await dbContext.Channels
            .SelectOneAsync(c => c.Id, c => c.Id == deleteChannel.ChannelId);
        return maybeChannel.ToValidation<BaseError>($"Channel {deleteChannel.ChannelId} does not exist.");
    }
}
