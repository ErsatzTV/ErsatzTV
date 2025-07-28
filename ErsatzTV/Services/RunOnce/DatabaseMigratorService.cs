using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Infrastructure.Data;
using Microsoft.Data.Sqlite;
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        _logger.LogInformation("Applying database migrations");

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        await using TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

        List<string> pendingMigrations = await dbContext.Database
            .GetPendingMigrationsAsync(stoppingToken)
            .Map(l => l.ToList());

        if (pendingMigrations.Contains("Add_MediaFilePathHash", StringComparer.OrdinalIgnoreCase))
        {
            await dbContext.Database.MigrateAsync("Add_MediaFilePathHash", stoppingToken);
        }

        List<string> appliedMigrations = await dbContext.Database
            .GetAppliedMigrationsAsync(stoppingToken)
            .Map(l => l.ToList());

        if (appliedMigrations.Count > 0)
        {
            // this can't be part of a migration, so we have to stop here and run some sql
            await PopulatePathHashes(dbContext);
        }

        // then continue migrating
        await dbContext.Database.MigrateAsync(stoppingToken);

        await DbInitializer.Initialize(dbContext, stoppingToken);

        _systemStartup.DatabaseIsReady();

        _logger.LogInformation("Done applying database migrations");
    }

    private static async Task PopulatePathHashes(TvContext dbContext)
    {
        if (await dbContext.Connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM `MediaFile` WHERE `PathHash` IS NULL OR `PathHash` = ''") == 0)
        {
            return;
        }

        if (dbContext.Connection is SqliteConnection sqliteConnection)
        {
            sqliteConnection.CreateFunction("HASH_SHA256", (string text) => PathUtils.GetPathHash(text));
            await dbContext.Connection.ExecuteAsync("UPDATE `MediaFile` SET `PathHash` = HASH_SHA256(`Path`);");
        }
        else
        {
            // mysql
            await dbContext.Connection.ExecuteAsync("UPDATE `MediaFile` SET `PathHash` = sha2(`Path`, 256);");
        }
    }
}
