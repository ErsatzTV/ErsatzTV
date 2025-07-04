using ErsatzTV.Core;
using ErsatzTV.Core.Health;

namespace ErsatzTV.Services.RunOnce;

public class RunHealthChecksService(IServiceScopeFactory serviceScopeFactory, SystemStartup systemStartup)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        await systemStartup.WaitForDatabase(stoppingToken);
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        await systemStartup.WaitForDatabaseCleaned(stoppingToken);
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        await systemStartup.WaitForSearchIndex(stoppingToken);
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IHealthCheckService healthCheckService = scope.ServiceProvider.GetRequiredService<IHealthCheckService>();
        await healthCheckService.PerformHealthChecks(stoppingToken);
    }
}
