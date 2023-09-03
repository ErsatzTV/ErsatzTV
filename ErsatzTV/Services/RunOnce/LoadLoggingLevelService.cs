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

        Option<LogEventLevel> maybeLogLevel =
            await configElementRepository.GetValue<LogEventLevel>(ConfigElementKey.MinimumLogLevel);
        foreach (LogEventLevel logLevel in maybeLogLevel)
        {
            LoggingLevelSwitch loggingLevelSwitch = scope.ServiceProvider.GetRequiredService<LoggingLevelSwitch>();
            loggingLevelSwitch.MinimumLevel = logLevel;
        }
    }
}
