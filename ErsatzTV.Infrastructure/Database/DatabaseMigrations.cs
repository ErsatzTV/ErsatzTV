using ErsatzTV.Core.Interfaces.Database;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Database;

public class DatabaseMigrations(IDbContextFactory<TvContext> dbContextFactory, ILogger<DatabaseMigrations> logger)
    : IDatabaseMigrations
{
    private IReadOnlyList<string> _unknownMigrations;

    public async Task<IReadOnlyList<string>> GetUnknownMigrations()
    {
        if (_unknownMigrations is not null)
        {
            return _unknownMigrations;
        }

        try
        {
            await using TvContext context = await dbContextFactory.CreateDbContextAsync();

            IEnumerable<string> appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            IEnumerable<string> definedMigrations = context.Database.GetMigrations();

            _unknownMigrations = appliedMigrations.Except(definedMigrations).ToList().AsReadOnly();
            if (_unknownMigrations.Any())
            {
                logger.LogCritical(
                    "Downgrade detected! Database has migrations not known to this version of ErsatzTV: {Migrations}",
                    _unknownMigrations);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Error checking for downgrade-related database migrations");
            _unknownMigrations = [];
        }

        return _unknownMigrations;
    }
}
