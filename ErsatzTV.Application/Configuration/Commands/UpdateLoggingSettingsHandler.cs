using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Configuration;

public class UpdateLoggingSettingsHandler : IRequestHandler<UpdateLoggingSettings, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly LoggingLevelSwitches _loggingLevelSwitches;

    public UpdateLoggingSettingsHandler(
        LoggingLevelSwitches loggingLevelSwitches,
        IConfigElementRepository configElementRepository)
    {
        _loggingLevelSwitches = loggingLevelSwitches;
        _configElementRepository = configElementRepository;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        UpdateLoggingSettings request,
        CancellationToken cancellationToken) => await ApplyUpdate(request.LoggingSettings);

    private async Task<Unit> ApplyUpdate(LoggingSettingsViewModel loggingSettings)
    {
        await _configElementRepository.Upsert(ConfigElementKey.MinimumLogLevel, loggingSettings.DefaultMinimumLogLevel);
        _loggingLevelSwitches.DefaultLevelSwitch.MinimumLevel = loggingSettings.DefaultMinimumLogLevel;

        await _configElementRepository.Upsert(
            ConfigElementKey.MinimumLogLevelScanning,
            loggingSettings.ScanningMinimumLogLevel);
        _loggingLevelSwitches.ScanningLevelSwitch.MinimumLevel = loggingSettings.ScanningMinimumLogLevel;

        await _configElementRepository.Upsert(
            ConfigElementKey.MinimumLogLevelScheduling,
            loggingSettings.SchedulingMinimumLogLevel);
        _loggingLevelSwitches.SchedulingLevelSwitch.MinimumLevel = loggingSettings.SchedulingMinimumLogLevel;

        await _configElementRepository.Upsert(
            ConfigElementKey.MinimumLogLevelSearching,
            loggingSettings.SearchingMinimumLogLevel);
        _loggingLevelSwitches.SearchingLevelSwitch.MinimumLevel = loggingSettings.SearchingMinimumLogLevel;

        await _configElementRepository.Upsert(
            ConfigElementKey.MinimumLogLevelStreaming,
            loggingSettings.StreamingMinimumLogLevel);
        _loggingLevelSwitches.StreamingLevelSwitch.MinimumLevel = loggingSettings.StreamingMinimumLogLevel;

        await _configElementRepository.Upsert(
            ConfigElementKey.MinimumLogLevelHttp,
            loggingSettings.HttpMinimumLogLevel);
        _loggingLevelSwitches.HttpLevelSwitch.MinimumLevel = loggingSettings.HttpMinimumLogLevel;

        return Unit.Default;
    }
}
