using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Runtime;
using ErsatzTV.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Services.RunOnce;

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
        if (runtimeInfo != null && runtimeInfo.IsOSPlatform(OSPlatform.Linux) &&
            System.IO.Directory.Exists("/dev/dri"))
        {
            ILocalFileSystem localFileSystem = scope.ServiceProvider.GetRequiredService<ILocalFileSystem>();
            IMemoryCache memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

            var devices = localFileSystem.ListFiles("/dev/dri")
                .Filter(s => s.StartsWith("/dev/dri/render"))
                .ToList();

            memoryCache.Set("ffmpeg.render_devices", devices);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}