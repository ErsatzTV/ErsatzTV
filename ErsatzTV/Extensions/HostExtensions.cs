using System;
using System.IO;
using System.Linq;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Extensions
{
    public static class HostExtensions
    {
        public static IHost SeedDatabase(this IHost host)
        {
            Unit _ = use(() => host.Services.CreateScope(), Seed);
            return host;
        }

        public static IHost CleanCacheFolder(this IHost host)
        {
            Unit _ = use(() => host.Services.CreateScope(), CleanCache);
            return host;
        }

        private static Unit Seed(IServiceScope scope) =>
            Try(() => scope.ServiceProvider)
                .Bind(services => Try(GetDbContext(services)))
                .Bind(ctx => Try(Migrate(ctx, scope.ServiceProvider)))
                .Bind(ctx => Try(InitializeDb(ctx)))
                .IfFail(
                    ex =>
                    {
                        LogException(
                            ex,
                            "Error occured while migrating database; shutting down.",
                            scope.ServiceProvider);
                        Environment.Exit(13);
                        return unit;
                    });

        private static Unit CleanCache(IServiceScope scope) =>
            Try(() => scope.ServiceProvider)
                .Bind(services => Try(GetDbContext(services)))
                .Bind(ctx => Try(CleanCache(ctx, scope.ServiceProvider)))
                .IfFail(ex => LogException(ex, "Error occured while cleaning cache", scope.ServiceProvider));

        private static TvContext GetDbContext(IServiceProvider provider) =>
            provider.GetRequiredService<TvContext>();

        private static TvContext Migrate(TvContext context, IServiceProvider provider)
        {
            ILogger<Program> logger = provider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Applying database migrations");
            context.Database.Migrate();
            logger.LogInformation("Done applying database migrations");
            return context;
        }

        private static Unit InitializeDb(TvContext context) =>
            DbInitializer.Initialize(context);

        private static Unit CleanCache(TvContext context, IServiceProvider provider)
        {
            if (Directory.Exists(FileSystemLayout.LegacyImageCacheFolder))
            {
                ILogger<Program> logger = provider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Migrating channel logos from legacy image cache folder");

                var logos = context.Channels
                    .SelectMany(c => c.Artwork)
                    .Where(a => a.ArtworkKind == ArtworkKind.Logo)
                    .Map(a => a.Path)
                    .ToList();

                ILocalFileSystem localFileSystem = provider.GetRequiredService<ILocalFileSystem>();
                foreach (string logo in logos)
                {
                    string legacyPath = Path.Combine(FileSystemLayout.LegacyImageCacheFolder, logo);
                    if (File.Exists(legacyPath))
                    {
                        string subfolder = logo.Substring(0, 2);
                        string newPath = Path.Combine(FileSystemLayout.LogoCacheFolder, subfolder, logo);
                        localFileSystem.CopyFile(legacyPath, newPath);
                    }
                }

                logger.LogInformation("Deleting legacy image cache folder");
                Directory.Delete(FileSystemLayout.LegacyImageCacheFolder, true);
            }

            return Unit.Default;
        }

        private static Unit LogException(Exception ex, string message, IServiceProvider provider)
        {
            provider.GetRequiredService<ILogger<Program>>().LogError(ex, message);
            return unit;
        }
    }
}
