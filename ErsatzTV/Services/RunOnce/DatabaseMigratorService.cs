using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Services.RunOnce
{
    public class DatabaseMigratorService : IHostedService
    {
        private readonly ILogger<DatabaseMigratorService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DatabaseMigratorService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DatabaseMigratorService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Applying database migrations");

            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            await using TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
            await DbInitializer.Initialize(dbContext, cancellationToken);

            _logger.LogInformation("Done applying database migrations");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
