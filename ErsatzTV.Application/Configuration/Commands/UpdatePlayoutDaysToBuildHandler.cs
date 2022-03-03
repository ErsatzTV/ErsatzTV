using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Configuration;

public class
    UpdatePlayoutDaysToBuildHandler : MediatR.IRequestHandler<UpdatePlayoutDaysToBuild, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

    public UpdatePlayoutDaysToBuildHandler(
        IConfigElementRepository configElementRepository,
        IDbContextFactory<TvContext> dbContextFactory,
        ChannelWriter<IBackgroundServiceRequest> workerChannel)
    {
        _configElementRepository = configElementRepository;
        _dbContextFactory = dbContextFactory;
        _workerChannel = workerChannel;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        UpdatePlayoutDaysToBuild request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Validation<BaseError, Unit> validation = await Validate(request);
        return await validation.Apply<Unit, Unit>(_ => ApplyUpdate(dbContext, request.DaysToBuild));
    }

    private async Task<Unit> ApplyUpdate(TvContext dbContext, int daysToBuild)
    {
        await _configElementRepository.Upsert(ConfigElementKey.PlayoutDaysToBuild, daysToBuild);
            
        // build all playouts to proper number of days
        List<Playout> playouts = await dbContext.Playouts
            .Include(p => p.Channel)
            .ToListAsync();
        foreach (int playoutId in playouts.OrderBy(p => decimal.Parse(p.Channel.Number)).Map(p => p.Id))
        {
            await _workerChannel.WriteAsync(new BuildPlayout(playoutId));
        }
            
        return Unit.Default;
    }

    private static Task<Validation<BaseError, Unit>> Validate(UpdatePlayoutDaysToBuild request) =>
        Optional(request.DaysToBuild)
            .Where(days => days > 0)
            .Map(_ => Unit.Default)
            .ToValidation<BaseError>("Days to build must be greater than zero")
            .AsTask();
}