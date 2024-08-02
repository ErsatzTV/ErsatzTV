using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Infrastructure.Data;

namespace ErsatzTV.Services.RunOnce;

public class DatabaseCleanerService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DatabaseCleanerService> logger,
    SystemStartup systemStartup)
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

        logger.LogInformation("Cleaning database");

        using IServiceScope scope = serviceScopeFactory.CreateScope();
        await using TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

        // some old version deleted items in a way that MediaItem was left over without
        // any corresponding Movie/Show/etc.
        // this cleans out that old invalid data
        await dbContext.Connection.ExecuteAsync(
            """
            delete
            from MediaItem
            where Id not in (select Id from Movie)
              and Id not in (select Id from Show)
              and Id not in (select Id from Season)
              and Id not in (select Id from Episode)
              and Id not in (select Id from OtherVideo)
              and Id not in (select Id from MusicVideo)
              and Id not in (select Id from Song)
              and Id not in (select Id from Artist)
              and Id not in (select Id from Image)
            """);

        systemStartup.DatabaseIsCleaned();

        logger.LogInformation("Done cleaning database");
    }
}
