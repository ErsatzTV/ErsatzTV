using System.Threading.Channels;
using ErsatzTV.Application.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Channel = ErsatzTV.Core.Domain.Channel;

namespace ErsatzTV.Application.Playouts;

public class CreateSequentialPlayoutHandler
    : IRequestHandler<CreateSequentialPlayout, Either<BaseError, CreatePlayoutResponse>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;

    public CreateSequentialPlayoutHandler(
        ILocalFileSystem localFileSystem,
        ChannelWriter<IBackgroundServiceRequest> channel,
        IDbContextFactory<TvContext> dbContextFactory)
    {
        _localFileSystem = localFileSystem;
        _channel = channel;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Either<BaseError, CreatePlayoutResponse>> Handle(
        CreateSequentialPlayout request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playout> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(playout => PersistPlayout(dbContext, playout, cancellationToken));
    }

    private async Task<CreatePlayoutResponse> PersistPlayout(
        TvContext dbContext,
        Playout playout,
        CancellationToken cancellationToken)
    {
        await dbContext.Playouts.AddAsync(playout, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await _channel.WriteAsync(new BuildPlayout(playout.Id, PlayoutBuildMode.Reset), cancellationToken);
        if (playout.Channel.PlayoutMode is ChannelPlayoutMode.OnDemand)
        {
            await _channel.WriteAsync(
                new TimeShiftOnDemandPlayout(playout.Id, DateTimeOffset.Now, false),
                cancellationToken);
        }

        await _channel.WriteAsync(new RefreshChannelList(), cancellationToken);
        return new CreatePlayoutResponse(playout.Id);
    }

    private async Task<Validation<BaseError, Playout>> Validate(
        TvContext dbContext,
        CreateSequentialPlayout request,
        CancellationToken cancellationToken) =>
        (await ValidateChannel(dbContext, request, cancellationToken), ValidateYamlFile(request), ValidateScheduleKind(request))
        .Apply((channel, yamlFile, scheduleKind) => new Playout
        {
            ChannelId = channel.Id,
            ScheduleFile = yamlFile,
            ScheduleKind = scheduleKind,
            Seed = new Random().Next()
        });

    private static Task<Validation<BaseError, Channel>> ValidateChannel(
        TvContext dbContext,
        CreateSequentialPlayout createSequentialPlayout,
        CancellationToken cancellationToken) =>
        dbContext.Channels
            .Include(c => c.Playouts)
            .SelectOneAsync(c => c.Id, c => c.Id == createSequentialPlayout.ChannelId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Channel does not exist"))
            .BindT(ChannelMustNotHavePlayouts);

    private static Validation<BaseError, Channel> ChannelMustNotHavePlayouts(Channel channel) =>
        Optional(channel.Playouts.Count)
            .Filter(count => count == 0)
            .Map(_ => channel)
            .ToValidation<BaseError>("Channel already has one playout");

    private Validation<BaseError, string> ValidateYamlFile(CreateSequentialPlayout request)
    {
        if (!_localFileSystem.FileExists(request.ScheduleFile))
        {
            return BaseError.New("Sequential schedule does not exist!");
        }

        return request.ScheduleFile;
    }

    private static Validation<BaseError, PlayoutScheduleKind> ValidateScheduleKind(
        CreateSequentialPlayout createSequentialPlayout) =>
        Optional(createSequentialPlayout.ScheduleKind)
            .Filter(scheduleKind => scheduleKind == PlayoutScheduleKind.Sequential)
            .ToValidation<BaseError>("[ScheduleKind] must be Sequential");
}
