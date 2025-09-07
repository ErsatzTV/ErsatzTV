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

public class CreateClassicPlayoutHandler : IRequestHandler<CreateClassicPlayout, Either<BaseError, CreatePlayoutResponse>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public CreateClassicPlayoutHandler(
        ChannelWriter<IBackgroundServiceRequest> channel,
        IDbContextFactory<TvContext> dbContextFactory)
    {
        _channel = channel;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Either<BaseError, CreatePlayoutResponse>> Handle(
        CreateClassicPlayout request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playout> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(playout => PersistPlayout(dbContext, playout));
    }

    private async Task<CreatePlayoutResponse> PersistPlayout(TvContext dbContext, Playout playout)
    {
        await dbContext.Playouts.AddAsync(playout);
        await dbContext.SaveChangesAsync();
        await _channel.WriteAsync(new BuildPlayout(playout.Id, PlayoutBuildMode.Reset));
        if (playout.Channel.PlayoutMode is ChannelPlayoutMode.OnDemand)
        {
            await _channel.WriteAsync(new TimeShiftOnDemandPlayout(playout.Id, DateTimeOffset.Now, false));
        }

        await _channel.WriteAsync(new RefreshChannelList());
        return new CreatePlayoutResponse(playout.Id);
    }

    private static async Task<Validation<BaseError, Playout>> Validate(
        TvContext dbContext,
        CreateClassicPlayout request,
        CancellationToken cancellationToken) =>
        (await ValidateChannel(dbContext, request, cancellationToken),
            await ValidateProgramSchedule(dbContext, request, cancellationToken),
            ValidateScheduleKind(request))
        .Apply((channel, programSchedule, scheduleKind) => new Playout
        {
            ChannelId = channel.Id,
            ProgramScheduleId = programSchedule.Id,
            ScheduleKind = scheduleKind
        });

    private static Task<Validation<BaseError, Channel>> ValidateChannel(
        TvContext dbContext,
        CreateClassicPlayout createClassicPlayout,
        CancellationToken cancellationToken) =>
        dbContext.Channels
            .Include(c => c.Playouts)
            .SelectOneAsync(c => c.Id, c => c.Id == createClassicPlayout.ChannelId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Channel does not exist"))
            .BindT(ChannelMustNotHavePlayouts);

    private static Validation<BaseError, Channel> ChannelMustNotHavePlayouts(Channel channel) =>
        Optional(channel.Playouts.Count)
            .Filter(count => count == 0)
            .Map(_ => channel)
            .ToValidation<BaseError>("Channel already has one playout");

    private static Task<Validation<BaseError, ProgramSchedule>> ValidateProgramSchedule(
        TvContext dbContext,
        CreateClassicPlayout createClassicPlayout,
        CancellationToken cancellationToken) =>
        dbContext.ProgramSchedules
            .Include(ps => ps.Items)
            .SelectOneAsync(ps => ps.Id, ps => ps.Id == createClassicPlayout.ProgramScheduleId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Program schedule does not exist"))
            .BindT(ProgramScheduleMustHaveItems);

    private static Validation<BaseError, ProgramSchedule> ProgramScheduleMustHaveItems(
        ProgramSchedule programSchedule) =>
        Optional(programSchedule)
            .Filter(ps => ps.Items.Count != 0)
            .ToValidation<BaseError>("Program schedule must have items");

    private static Validation<BaseError, PlayoutScheduleKind> ValidateScheduleKind(
        CreateClassicPlayout createClassicPlayout) =>
        Optional(createClassicPlayout.ScheduleKind)
            .Filter(scheduleKind => scheduleKind == PlayoutScheduleKind.Classic)
            .ToValidation<BaseError>("[ScheduleKind] must be Classic");
}
