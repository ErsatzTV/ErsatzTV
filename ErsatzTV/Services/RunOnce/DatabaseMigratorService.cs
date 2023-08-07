using ErsatzTV.Core;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Services.RunOnce;

public class DatabaseMigratorService : BackgroundService
{
    private readonly ILogger<DatabaseMigratorService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SystemStartup _systemStartup;

    public DatabaseMigratorService(
        IServiceScopeFactory serviceScopeFactory,
        SystemStartup systemStartup,
        ILogger<DatabaseMigratorService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _systemStartup = systemStartup;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        _logger.LogInformation("Applying database migrations");

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        await using TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await DbInitializer.Initialize(dbContext, cancellationToken);

        _systemStartup.DatabaseIsReady();

        _logger.LogInformation("Done applying database migrations");
    }
}
