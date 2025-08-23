using System.Threading.Channels;
using ErsatzTV.Application.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Configuration;

public class UpdateXmltvSettingsHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IConfigElementRepository configElementRepository,
    ChannelWriter<IBackgroundServiceRequest> workerChannel)
    : IRequestHandler<UpdateXmltvSettings, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        UpdateXmltvSettings request,
        CancellationToken cancellationToken)
    {
        int playoutDaysToBuild =
            await configElementRepository
                .GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
                .IfNoneAsync(2);

        if (playoutDaysToBuild < request.XmltvSettings.DaysToBuild)
        {
            return BaseError.New(
                $"XMLTV days to build ({request.XmltvSettings.DaysToBuild}) cannot be greater than Playout days to build ({playoutDaysToBuild})");
        }

        return await ApplyUpdate(request.XmltvSettings);
    }

    private async Task<Unit> ApplyUpdate(XmltvSettingsViewModel xmltvSettings)
    {
        await configElementRepository.Upsert(ConfigElementKey.XmltvTimeZone, xmltvSettings.TimeZone);
        await configElementRepository.Upsert(ConfigElementKey.XmltvDaysToBuild, xmltvSettings.DaysToBuild);
        await configElementRepository.Upsert(ConfigElementKey.XmltvBlockBehavior, xmltvSettings.BlockBehavior);

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();

        foreach (string channelNumber in await dbContext.Channels.Map(c => c.Number).ToListAsync())
        {
            await workerChannel.WriteAsync(new RefreshChannelData(channelNumber));
        }

        return Unit.Default;
    }
}
