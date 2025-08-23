using System.Threading.Channels;
using ErsatzTV.Application.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Channel = ErsatzTV.Core.Domain.Channel;

namespace ErsatzTV.Application.Playouts;

public class CreateBlockPlayoutHandler(
    ChannelWriter<IBackgroundServiceRequest> channel,
    IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreateBlockPlayout, Either<BaseError, CreatePlayoutResponse>>
{
    public async Task<Either<BaseError, CreatePlayoutResponse>> Handle(
        CreateBlockPlayout request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playout> validation = await Validate(dbContext, request);
        return await validation.Apply(playout => PersistPlayout(dbContext, playout));
    }

    private async Task<CreatePlayoutResponse> PersistPlayout(TvContext dbContext, Playout playout)
    {
        await dbContext.Playouts.AddAsync(playout);
        await dbContext.SaveChangesAsync();
        await channel.WriteAsync(new BuildPlayout(playout.Id, PlayoutBuildMode.Reset));
        if (playout.Channel.PlayoutMode is ChannelPlayoutMode.OnDemand)
        {
            await channel.WriteAsync(new TimeShiftOnDemandPlayout(playout.Id, DateTimeOffset.Now, false));
        }

        await channel.WriteAsync(new RefreshChannelList());
        return new CreatePlayoutResponse(playout.Id);
    }

    private static async Task<Validation<BaseError, Playout>> Validate(
        TvContext dbContext,
        CreateBlockPlayout request) =>
        (await ValidateChannel(dbContext, request), ValidateScheduleKind(request))
        .Apply((channel, scheduleKind) => new Playout
        {
            ChannelId = channel.Id,
            ScheduleKind = scheduleKind,
            Seed = new Random().Next()
        });

    private static Task<Validation<BaseError, Channel>> ValidateChannel(
        TvContext dbContext,
        CreateBlockPlayout createBlockPlayout) =>
        dbContext.Channels
            .Include(c => c.Playouts)
            .SelectOneAsync(c => c.Id, c => c.Id == createBlockPlayout.ChannelId)
            .Map(o => o.ToValidation<BaseError>("Channel does not exist"))
            .BindT(ChannelMustNotHavePlayouts);

    private static Validation<BaseError, Channel> ChannelMustNotHavePlayouts(Channel channel) =>
        Optional(channel.Playouts.Count)
            .Filter(count => count == 0)
            .Map(_ => channel)
            .ToValidation<BaseError>("Channel already has one playout");

    private static Validation<BaseError, PlayoutScheduleKind> ValidateScheduleKind(
        CreateBlockPlayout createBlockPlayout) =>
        Optional(createBlockPlayout.ScheduleKind)
            .Filter(scheduleKind => scheduleKind == PlayoutScheduleKind.Block)
            .ToValidation<BaseError>("[ScheduleKind] must be Block");
}
