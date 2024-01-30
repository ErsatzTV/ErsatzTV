using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Configuration;

public class UpdateGeneralSettingsHandler : IRequestHandler<UpdateGeneralSettings, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly LoggingLevelSwitches _loggingLevelSwitches;

    public UpdateGeneralSettingsHandler(
        LoggingLevelSwitches loggingLevelSwitches,
        IConfigElementRepository configElementRepository)
    {
        _loggingLevelSwitches = loggingLevelSwitches;
        _configElementRepository = configElementRepository;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        UpdateGeneralSettings request,
        CancellationToken cancellationToken) => await ApplyUpdate(request.GeneralSettings);

    private async Task<Unit> ApplyUpdate(GeneralSettingsViewModel generalSettings)
    {
        await _configElementRepository.Upsert(ConfigElementKey.MinimumLogLevel, generalSettings.DefaultMinimumLogLevel);
        _loggingLevelSwitches.DefaultLevelSwitch.MinimumLevel = generalSettings.DefaultMinimumLogLevel;

        await _configElementRepository.Upsert(ConfigElementKey.MinimumLogLevelScanning, generalSettings.ScanningMinimumLogLevel);
        _loggingLevelSwitches.ScanningLevelSwitch.MinimumLevel = generalSettings.ScanningMinimumLogLevel;

        await _configElementRepository.Upsert(ConfigElementKey.MinimumLogLevelScheduling, generalSettings.SchedulingMinimumLogLevel);
        _loggingLevelSwitches.SchedulingLevelSwitch.MinimumLevel = generalSettings.SchedulingMinimumLogLevel;
        
        await _configElementRepository.Upsert(ConfigElementKey.MinimumLogLevelStreaming, generalSettings.StreamingMinimumLogLevel);
        _loggingLevelSwitches.StreamingLevelSwitch.MinimumLevel = generalSettings.StreamingMinimumLogLevel;

        return Unit.Default;
    }
}
