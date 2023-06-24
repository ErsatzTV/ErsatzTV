using System.Runtime.InteropServices;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.FFmpeg.Runtime;
using Microsoft.Extensions.Caching.Memory;

namespace ErsatzTV.Services.RunOnce;

public class PlatformSettingsService : BackgroundService
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

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IRuntimeInfo runtimeInfo = scope.ServiceProvider.GetRequiredService<IRuntimeInfo>();
        if (runtimeInfo != null && runtimeInfo.IsOSPlatform(OSPlatform.Linux) &&
            Directory.Exists("/dev/dri"))
        {
            ILocalFileSystem localFileSystem = scope.ServiceProvider.GetRequiredService<ILocalFileSystem>();
            IMemoryCache memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

            var devices = localFileSystem.ListFiles("/dev/dri")
                .Filter(s => s.StartsWith("/dev/dri/render"))
                .ToList();

            memoryCache.Set("ffmpeg.render_devices", devices);
        }
    }
}
