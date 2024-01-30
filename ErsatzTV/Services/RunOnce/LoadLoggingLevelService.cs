using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Serilog.Core;
using Serilog.Events;

namespace ErsatzTV.Services.RunOnce;

public class LoadLoggingLevelService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SystemStartup _systemStartup;

    public LoadLoggingLevelService(IServiceScopeFactory serviceScopeFactory, SystemStartup systemStartup)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _systemStartup = systemStartup;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        await _systemStartup.WaitForDatabase(stoppingToken);
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IConfigElementRepository configElementRepository =
            scope.ServiceProvider.GetRequiredService<IConfigElementRepository>();

        foreach (LogEventLevel logLevel in await configElementRepository.GetValue<LogEventLevel>(ConfigElementKey.MinimumLogLevel))
        {
            LoggingLevelSwitches loggingLevelSwitches = scope.ServiceProvider.GetRequiredService<LoggingLevelSwitches>();
            loggingLevelSwitches.DefaultLevelSwitch.MinimumLevel = logLevel;
        }
        
        foreach (LogEventLevel logLevel in await configElementRepository.GetValue<LogEventLevel>(ConfigElementKey.MinimumLogLevelScanning))
        {
            LoggingLevelSwitches loggingLevelSwitches = scope.ServiceProvider.GetRequiredService<LoggingLevelSwitches>();
            loggingLevelSwitches.ScanningLevelSwitch.MinimumLevel = logLevel;
        }

        foreach (LogEventLevel logLevel in await configElementRepository.GetValue<LogEventLevel>(ConfigElementKey.MinimumLogLevelScheduling))
        {
            LoggingLevelSwitches loggingLevelSwitches = scope.ServiceProvider.GetRequiredService<LoggingLevelSwitches>();
            loggingLevelSwitches.SchedulingLevelSwitch.MinimumLevel = logLevel;
        }

        foreach (LogEventLevel logLevel in await configElementRepository.GetValue<LogEventLevel>(ConfigElementKey.MinimumLogLevelStreaming))
        {
            LoggingLevelSwitches loggingLevelSwitches = scope.ServiceProvider.GetRequiredService<LoggingLevelSwitches>();
            loggingLevelSwitches.StreamingLevelSwitch.MinimumLevel = logLevel;
        }
    }
}
