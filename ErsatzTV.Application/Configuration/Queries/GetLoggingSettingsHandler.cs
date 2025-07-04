using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Serilog.Events;

namespace ErsatzTV.Application.Configuration;

public class GetLoggingSettingsHandler : IRequestHandler<GetLoggingSettings, LoggingSettingsViewModel>
{
    private readonly IConfigElementRepository _configElementRepository;

    public GetLoggingSettingsHandler(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public async Task<LoggingSettingsViewModel> Handle(GetLoggingSettings request, CancellationToken cancellationToken)
    {
        Option<LogEventLevel> maybeDefaultLevel =
            await _configElementRepository.GetValue<LogEventLevel>(ConfigElementKey.MinimumLogLevel);

        Option<LogEventLevel> maybeScanningLevel =
            await _configElementRepository.GetValue<LogEventLevel>(ConfigElementKey.MinimumLogLevelScanning);

        Option<LogEventLevel> maybeSchedulingLevel =
            await _configElementRepository.GetValue<LogEventLevel>(ConfigElementKey.MinimumLogLevelScheduling);

        Option<LogEventLevel> maybeStreamingLevel =
            await _configElementRepository.GetValue<LogEventLevel>(ConfigElementKey.MinimumLogLevelStreaming);

        Option<LogEventLevel> maybeHttpLevel =
            await _configElementRepository.GetValue<LogEventLevel>(ConfigElementKey.MinimumLogLevelHttp);

        return new LoggingSettingsViewModel
        {
            DefaultMinimumLogLevel = await maybeDefaultLevel.IfNoneAsync(LogEventLevel.Information),
            ScanningMinimumLogLevel = await maybeScanningLevel.IfNoneAsync(LogEventLevel.Information),
            SchedulingMinimumLogLevel = await maybeSchedulingLevel.IfNoneAsync(LogEventLevel.Information),
            StreamingMinimumLogLevel = await maybeStreamingLevel.IfNoneAsync(LogEventLevel.Information),
            HttpMinimumLogLevel = await maybeHttpLevel.IfNoneAsync(LogEventLevel.Information)
        };
    }
}
