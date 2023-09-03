using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Services.RunOnce;

public class CacheCleanerService : BackgroundService
{
    private readonly ILogger<CacheCleanerService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SystemStartup _systemStartup;

    public CacheCleanerService(
        IServiceScopeFactory serviceScopeFactory,
        SystemStartup systemStartup,
        ILogger<CacheCleanerService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _systemStartup = systemStartup;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        await _systemStartup.WaitForDatabase(stoppingToken);
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        await using TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();
        ILocalFileSystem localFileSystem = scope.ServiceProvider.GetRequiredService<ILocalFileSystem>();

        if (localFileSystem.FolderExists(FileSystemLayout.LegacyImageCacheFolder))
        {
            _logger.LogInformation("Migrating channel logos from legacy image cache folder");

            List<string> logos = await dbContext.Channels
                .AsNoTracking()
                .SelectMany(c => c.Artwork)
                .Where(a => a.ArtworkKind == ArtworkKind.Logo)
                .Map(a => a.Path)
                .ToListAsync(stoppingToken);

            foreach (string logo in logos)
            {
                string legacyPath = Path.Combine(FileSystemLayout.LegacyImageCacheFolder, logo);
                if (localFileSystem.FileExists(legacyPath))
                {
                    string subfolder = logo[..2];
                    string newPath = Path.Combine(FileSystemLayout.LogoCacheFolder, subfolder, logo);
                    await localFileSystem.CopyFile(legacyPath, newPath);
                }
            }

            _logger.LogInformation("Deleting legacy image cache folder");
            Directory.Delete(FileSystemLayout.LegacyImageCacheFolder, true);
        }

        if (localFileSystem.FolderExists(FileSystemLayout.TranscodeFolder))
        {
            _logger.LogInformation("Emptying transcode cache folder");
            localFileSystem.EmptyFolder(FileSystemLayout.TranscodeFolder);
            _logger.LogInformation("Done emptying transcode cache folder");
        }

        if (localFileSystem.FolderExists(FileSystemLayout.ChannelGuideCacheFolder))
        {
            _logger.LogInformation("Cleaning channel cache");

            List<string> channelFiles = await dbContext.Channels
                .AsNoTracking()
                .Select(c => c.Number)
                .Map(num => Path.Combine(FileSystemLayout.ChannelGuideCacheFolder, $"{num}.xml"))
                .ToListAsync(stoppingToken);

            foreach (string fileName in localFileSystem.ListFiles(FileSystemLayout.ChannelGuideCacheFolder))
            {
                if (fileName.Contains("channels") || channelFiles.Contains(fileName))
                {
                    continue;
                }
                
                File.Delete(fileName);
            }
        }
    }
}
