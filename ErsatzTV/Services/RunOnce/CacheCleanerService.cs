using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Services.RunOnce;

public class CacheCleanerService : IHostedService
{
    private readonly ILogger<CacheCleanerService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CacheCleanerService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CacheCleanerService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        await using TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

        if (Directory.Exists(FileSystemLayout.LegacyImageCacheFolder))
        {
            _logger.LogInformation("Migrating channel logos from legacy image cache folder");

            List<string> logos = await dbContext.Channels
                .SelectMany(c => c.Artwork)
                .Where(a => a.ArtworkKind == ArtworkKind.Logo)
                .Map(a => a.Path)
                .ToListAsync(cancellationToken);

            ILocalFileSystem localFileSystem = scope.ServiceProvider.GetRequiredService<ILocalFileSystem>();
            foreach (string logo in logos)
            {
                string legacyPath = Path.Combine(FileSystemLayout.LegacyImageCacheFolder, logo);
                if (File.Exists(legacyPath))
                {
                    string subfolder = logo.Substring(0, 2);
                    string newPath = Path.Combine(FileSystemLayout.LogoCacheFolder, subfolder, logo);
                    await localFileSystem.CopyFile(legacyPath, newPath);
                }
            }

            _logger.LogInformation("Deleting legacy image cache folder");
            Directory.Delete(FileSystemLayout.LegacyImageCacheFolder, true);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}