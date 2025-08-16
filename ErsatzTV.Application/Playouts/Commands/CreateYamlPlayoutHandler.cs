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

public class CreateYamlPlayoutHandler
    : IRequestHandler<CreateYamlPlayout, Either<BaseError, CreatePlayoutResponse>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;

    public CreateYamlPlayoutHandler(
        ILocalFileSystem localFileSystem,
        ChannelWriter<IBackgroundServiceRequest> channel,
        IDbContextFactory<TvContext> dbContextFactory)
    {
        _localFileSystem = localFileSystem;
        _channel = channel;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Either<BaseError, CreatePlayoutResponse>> Handle(
        CreateYamlPlayout request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playout> validation = await Validate(dbContext, request);
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

    private async Task<Validation<BaseError, Playout>> Validate(
        TvContext dbContext,
        CreateYamlPlayout request) =>
        (await ValidateChannel(dbContext, request), ValidateYamlFile(request), ValidatePlayoutType(request))
        .Apply((channel, externalJsonFile, playoutType) => new Playout
        {
            ChannelId = channel.Id,
            TemplateFile = externalJsonFile,
            ProgramSchedulePlayoutType = playoutType,
            Seed = new Random().Next()
        });

    private static Task<Validation<BaseError, Channel>> ValidateChannel(
        TvContext dbContext,
        CreateYamlPlayout createYamlPlayout) =>
        dbContext.Channels
            .Include(c => c.Playouts)
            .SelectOneAsync(c => c.Id, c => c.Id == createYamlPlayout.ChannelId)
            .Map(o => o.ToValidation<BaseError>("Channel does not exist"))
            .BindT(ChannelMustNotHavePlayouts);

    private static Validation<BaseError, Channel> ChannelMustNotHavePlayouts(Channel channel) =>
        Optional(channel.Playouts.Count)
            .Filter(count => count == 0)
            .Map(_ => channel)
            .ToValidation<BaseError>("Channel already has one playout");

    private Validation<BaseError, string> ValidateYamlFile(CreateYamlPlayout request)
    {
        if (!_localFileSystem.FileExists(request.TemplateFile))
        {
            return BaseError.New("YAML file does not exist!");
        }

        return request.TemplateFile;
    }

    private static Validation<BaseError, ProgramSchedulePlayoutType> ValidatePlayoutType(
        CreateYamlPlayout createYamlPlayout) =>
        Optional(createYamlPlayout.ProgramSchedulePlayoutType)
            .Filter(playoutType => playoutType == ProgramSchedulePlayoutType.Yaml)
            .ToValidation<BaseError>("[ProgramSchedulePlayoutType] must be YAML");
}
