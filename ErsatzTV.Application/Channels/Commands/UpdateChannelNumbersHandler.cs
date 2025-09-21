using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Channel = ErsatzTV.Core.Domain.Channel;

namespace ErsatzTV.Application.Channels;

public class UpdateChannelNumbersHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    ChannelWriter<IBackgroundServiceRequest> workerChannel)
    : IRequestHandler<UpdateChannelNumbers, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(UpdateChannelNumbers request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var numberUpdates = request.Channels.ToDictionary(c => c.Id, c => c.Number);
            var channelIds = numberUpdates.Keys;

            List<Channel> channelsToUpdate = await dbContext.Channels
                .Where(c => channelIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            // give every channel a non-conflicting number
            foreach (var channel in channelsToUpdate)
            {
                channel.Number = $"-{channel.Id}";
            }

            // save those changes
            await dbContext.SaveChangesAsync(cancellationToken);

            // give every channel the proper new number
            foreach (var channel in channelsToUpdate)
            {
                channel.Number = numberUpdates[channel.Id];
            }

            // save those changes
            await dbContext.SaveChangesAsync(cancellationToken);

            // commit the transaction
            await transaction.CommitAsync(cancellationToken);

            // update channel list and xmltv
            await workerChannel.WriteAsync(new RefreshChannelList(), cancellationToken);
            foreach (var channel in channelsToUpdate)
            {
                await workerChannel.WriteAsync(new RefreshChannelData(channel.Number), cancellationToken);
            }

            return Option<BaseError>.None;
        }
        catch (Exception ex)
        {
            return BaseError.New("Failed to update channel numbers: " + ex.Message);
        }
    }
}
