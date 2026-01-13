using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
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

        foreach (LogEventLevel logLevel in await configElementRepository.GetValue<LogEventLevel>(
                     ConfigElementKey.MinimumLogLevel, stoppingToken))
        {
            LoggingLevelSwitches loggingLevelSwitches =
                scope.ServiceProvider.GetRequiredService<LoggingLevelSwitches>();
            loggingLevelSwitches.DefaultLevelSwitch.MinimumLevel = logLevel;
        }

        foreach (LogEventLevel logLevel in await configElementRepository.GetValue<LogEventLevel>(
                     ConfigElementKey.MinimumLogLevelScanning, stoppingToken))
        {
            LoggingLevelSwitches loggingLevelSwitches =
                scope.ServiceProvider.GetRequiredService<LoggingLevelSwitches>();
            loggingLevelSwitches.ScanningLevelSwitch.MinimumLevel = logLevel;
        }

        foreach (LogEventLevel logLevel in await configElementRepository.GetValue<LogEventLevel>(
                     ConfigElementKey.MinimumLogLevelScheduling, stoppingToken))
        {
            LoggingLevelSwitches loggingLevelSwitches =
                scope.ServiceProvider.GetRequiredService<LoggingLevelSwitches>();
            loggingLevelSwitches.SchedulingLevelSwitch.MinimumLevel = logLevel;
        }

        foreach (LogEventLevel logLevel in await configElementRepository.GetValue<LogEventLevel>(
                     ConfigElementKey.MinimumLogLevelSearching, stoppingToken))
        {
            LoggingLevelSwitches loggingLevelSwitches =
                scope.ServiceProvider.GetRequiredService<LoggingLevelSwitches>();
            loggingLevelSwitches.SearchingLevelSwitch.MinimumLevel = logLevel;
        }

        foreach (LogEventLevel logLevel in await configElementRepository.GetValue<LogEventLevel>(
                     ConfigElementKey.MinimumLogLevelStreaming, stoppingToken))
        {
            LoggingLevelSwitches loggingLevelSwitches =
                scope.ServiceProvider.GetRequiredService<LoggingLevelSwitches>();
            loggingLevelSwitches.StreamingLevelSwitch.MinimumLevel = logLevel;
        }

        foreach (LogEventLevel logLevel in await configElementRepository.GetValue<LogEventLevel>(
                     ConfigElementKey.MinimumLogLevelHttp, stoppingToken))
        {
            LoggingLevelSwitches loggingLevelSwitches =
                scope.ServiceProvider.GetRequiredService<LoggingLevelSwitches>();
            loggingLevelSwitches.HttpLevelSwitch.MinimumLevel = logLevel;
        }
    }
}
