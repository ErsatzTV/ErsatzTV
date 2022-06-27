using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Serilog.Core;
using Serilog.Events;

namespace ErsatzTV.Services.RunOnce;

public class LoadLoggingLevelService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public LoadLoggingLevelService(IServiceScopeFactory serviceScopeFactory) =>
        _serviceScopeFactory = serviceScopeFactory;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
