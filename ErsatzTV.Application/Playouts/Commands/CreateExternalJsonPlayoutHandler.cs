using System.IO.Abstractions;
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

public class CreateExternalJsonPlayoutHandler
    : IRequestHandler<CreateExternalJsonPlayout, Either<BaseError, CreatePlayoutResponse>>
{
    private readonly IFileSystem _fileSystem;
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public CreateExternalJsonPlayoutHandler(
        IFileSystem fileSystem,
        ChannelWriter<IBackgroundServiceRequest> channel,
        IDbContextFactory<TvContext> dbContextFactory)
    {
        _fileSystem = fileSystem;
        _channel = channel;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Either<BaseError, CreatePlayoutResponse>> Handle(
        CreateExternalJsonPlayout request,
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
        await _channel.WriteAsync(new RefreshChannelList());
        return new CreatePlayoutResponse(playout.Id);
    }

    private async Task<Validation<BaseError, Playout>> Validate(
        TvContext dbContext,
        CreateExternalJsonPlayout request,
        CancellationToken cancellationToken) =>
        (await ValidateChannel(dbContext, request, cancellationToken), ValidateExternalJsonFile(request), ValidateScheduleKind(request))
        .Apply((channel, externalJsonFile, scheduleKind) => new Playout
        {
            ChannelId = channel.Id,
            ScheduleFile = externalJsonFile,
            ScheduleKind = scheduleKind
        });

    private static Task<Validation<BaseError, Channel>> ValidateChannel(
        TvContext dbContext,
        CreateExternalJsonPlayout createExternalJsonPlayout,
        CancellationToken cancellationToken) =>
        dbContext.Channels
            .Include(c => c.Playouts)
            .SelectOneAsync(c => c.Id, c => c.Id == createExternalJsonPlayout.ChannelId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Channel does not exist"))
            .BindT(ChannelMustNotHavePlayouts);

    private static Validation<BaseError, Channel> ChannelMustNotHavePlayouts(Channel channel) =>
        Optional(channel.Playouts.Count)
            .Filter(count => count == 0)
            .Map(_ => channel)
            .ToValidation<BaseError>("Channel already has one playout");

    private Validation<BaseError, string> ValidateExternalJsonFile(CreateExternalJsonPlayout request)
    {
        if (!_fileSystem.File.Exists(request.ScheduleFile))
        {
            return BaseError.New("External Json File does not exist!");
        }

        return request.ScheduleFile;
    }

    private static Validation<BaseError, PlayoutScheduleKind> ValidateScheduleKind(
        CreateExternalJsonPlayout createExternalJsonPlayout) =>
        Optional(createExternalJsonPlayout.ScheduleKind)
            .Filter(scheduleKind => scheduleKind == PlayoutScheduleKind.ExternalJson)
            .ToValidation<BaseError>("[ScheduleKind] must be ExternalJson");
}
