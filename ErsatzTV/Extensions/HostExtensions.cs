using System;
using System.IO;
using System.Linq;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
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
                .Bind(ctx => Try(Migrate(ctx)))
                .Bind(ctx => Try(InitializeDb(ctx)))
                .IfFail(ex => LogException(ex, "Error occured while seeding database", scope.ServiceProvider));

        private static Unit CleanCache(IServiceScope scope) =>
            Try(() => scope.ServiceProvider)
                .Bind(services => Try(GetDbContext(services)))
                .Bind(ctx => Try(CleanCache(ctx, scope.ServiceProvider)))
                .IfFail(ex => LogException(ex, "Error occured while cleaning cache", scope.ServiceProvider));

        private static TvContext GetDbContext(IServiceProvider provider) =>
            provider.GetRequiredService<TvContext>();

        private static TvContext Migrate(TvContext context)
        {
            context.Database.Migrate();
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

                foreach (string logo in logos)
                {
                    string legacyPath = Path.Combine(FileSystemLayout.LegacyImageCacheFolder, logo);
                    if (File.Exists(legacyPath))
                    {
                        string subfolder = logo.Substring(0, 2);
                        string newPath = Path.Combine(FileSystemLayout.LogoCacheFolder, subfolder, logo);
                        File.Copy(legacyPath, newPath, true);
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
