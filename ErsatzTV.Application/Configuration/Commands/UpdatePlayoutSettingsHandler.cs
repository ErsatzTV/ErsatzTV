using System.Globalization;
using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Configuration;

public class UpdatePlayoutSettingsHandler : IRequestHandler<UpdatePlayoutSettings, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

    public UpdatePlayoutSettingsHandler(
        IConfigElementRepository configElementRepository,
        IDbContextFactory<TvContext> dbContextFactory,
        ChannelWriter<IBackgroundServiceRequest> workerChannel)
    {
        _configElementRepository = configElementRepository;
        _dbContextFactory = dbContextFactory;
        _workerChannel = workerChannel;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        UpdatePlayoutSettings request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Unit> validation = await Validate(request);
        return await validation.Apply<Unit, Unit>(_ => ApplyUpdate(dbContext, request.PlayoutSettings));
    }

    private async Task<Unit> ApplyUpdate(TvContext dbContext, PlayoutSettingsViewModel playoutSettings)
    {
        await _configElementRepository.Upsert(ConfigElementKey.PlayoutDaysToBuild, playoutSettings.DaysToBuild);
        await _configElementRepository.Upsert(
            ConfigElementKey.PlayoutSkipMissingItems,
            playoutSettings.SkipMissingItems);

        // continue all playouts to proper number of days
        List<Playout> playouts = await dbContext.Playouts
            .Include(p => p.Channel)
            .ToListAsync();
        foreach (int playoutId in playouts.OrderBy(p => decimal.Parse(p.Channel.Number, CultureInfo.InvariantCulture))
                     .Map(p => p.Id))
        {
            await _workerChannel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Continue));
        }

        return Unit.Default;
    }

    private static Task<Validation<BaseError, Unit>> Validate(UpdatePlayoutSettings request) =>
        Optional(request.PlayoutSettings.DaysToBuild)
            .Where(days => days > 0)
            .Map(_ => Unit.Default)
            .ToValidation<BaseError>("Days to build must be greater than zero")
            .AsTask();
}
