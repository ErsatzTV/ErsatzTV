using System.Runtime.InteropServices;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.FFmpeg.Runtime;
using Microsoft.Extensions.Caching.Memory;

namespace ErsatzTV.Services.RunOnce;

public class PlatformSettingsService(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IRuntimeInfo runtimeInfo = scope.ServiceProvider.GetRequiredService<IRuntimeInfo>();
        if (runtimeInfo != null && runtimeInfo.IsOSPlatform(OSPlatform.Linux) &&
            Directory.Exists("/dev/dri"))
        {
            ILocalFileSystem localFileSystem = scope.ServiceProvider.GetRequiredService<ILocalFileSystem>();
            IMemoryCache memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

            var devices = localFileSystem.ListFiles("/dev/dri")
                .Filter(
                    s => s.StartsWith("/dev/dri/render", StringComparison.OrdinalIgnoreCase)
                         || s.StartsWith("/dev/dri/card", StringComparison.OrdinalIgnoreCase))
                .ToList();

            memoryCache.Set("ffmpeg.render_devices", devices);
        }
    }
}
