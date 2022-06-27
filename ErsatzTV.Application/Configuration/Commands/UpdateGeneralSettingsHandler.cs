using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Serilog.Core;

namespace ErsatzTV.Application.Configuration;

public class UpdateGeneralSettingsHandler : IRequestHandler<UpdateGeneralSettings, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly LoggingLevelSwitch _loggingLevelSwitch;

    public UpdateGeneralSettingsHandler(
        LoggingLevelSwitch loggingLevelSwitch,
        IConfigElementRepository configElementRepository)
    {
        _loggingLevelSwitch = loggingLevelSwitch;
        _configElementRepository = configElementRepository;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        UpdateGeneralSettings request,
        CancellationToken cancellationToken) => await ApplyUpdate(request.GeneralSettings);

    private async Task<Unit> ApplyUpdate(GeneralSettingsViewModel generalSettings)
    {
        await _configElementRepository.Upsert(ConfigElementKey.MinimumLogLevel, generalSettings.MinimumLogLevel);
        _loggingLevelSwitch.MinimumLevel = generalSettings.MinimumLogLevel;

        return Unit.Default;
    }
}
