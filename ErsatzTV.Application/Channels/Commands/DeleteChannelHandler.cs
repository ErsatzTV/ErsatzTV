using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Channel = ErsatzTV.Core.Domain.Channel;

namespace ErsatzTV.Application.Channels;

public class DeleteChannelHandler : IRequestHandler<DeleteChannel, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

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
        Validation<BaseError, Channel> validation = await ChannelMustExist(dbContext, request);

        return await LanguageExtensions.Apply(validation, c => DoDeletion(dbContext, c, cancellationToken));
    }

    private async Task<Unit> DoDeletion(TvContext dbContext, Channel channel, CancellationToken cancellationToken)
    {
        dbContext.Channels.Remove(channel);
        await dbContext.SaveChangesAsync(cancellationToken);

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

    private static async Task<Validation<BaseError, Channel>> ChannelMustExist(
        TvContext dbContext,
        DeleteChannel deleteChannel)
    {
        Option<Channel> maybeChannel = await dbContext.Channels
            .SelectOneAsync(c => c.Id, c => c.Id == deleteChannel.ChannelId);
        return maybeChannel.ToValidation<BaseError>($"Channel {deleteChannel.ChannelId} does not exist.");
    }
}
