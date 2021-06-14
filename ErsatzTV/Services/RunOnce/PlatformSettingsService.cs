using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Runtime;
using ErsatzTV.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Services.RunOnce
{
    public class PlatformSettingsService : IHostedService
    {
        private readonly ILogger<PlatformSettingsService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public PlatformSettingsService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<PlatformSettingsService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            await using TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

            IRuntimeInfo runtimeInfo = scope.ServiceProvider.GetRequiredService<IRuntimeInfo>();
            if (runtimeInfo != null && runtimeInfo.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogInformation("Disabling ffmpeg reports on Windows platform");
                IConfigElementRepository repo = scope.ServiceProvider.GetRequiredService<IConfigElementRepository>();
                await repo.Upsert(ConfigElementKey.FFmpegSaveReports, false);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
