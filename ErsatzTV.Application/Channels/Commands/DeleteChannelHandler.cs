using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Channels;

public class DeleteChannelHandler : IRequestHandler<DeleteChannel, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public DeleteChannelHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Either<BaseError, Unit>> Handle(DeleteChannel request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Channel> validation = await ChannelMustExist(dbContext, request);
        return await validation.Apply(c => DoDeletion(dbContext, c));
    }

    private static async Task<Unit> DoDeletion(TvContext dbContext, Channel channel)
    {
        dbContext.Channels.Remove(channel);
        await dbContext.SaveChangesAsync();
        
        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Channel>> ChannelMustExist(TvContext dbContext, DeleteChannel deleteChannel)
    {
        Option<Channel> maybeChannel = await dbContext.Channels
            .SelectOneAsync(c => c.Id, c => c.Id == deleteChannel.ChannelId);
        return maybeChannel.ToValidation<BaseError>($"Channel {deleteChannel.ChannelId} does not exist.");
    }
}
