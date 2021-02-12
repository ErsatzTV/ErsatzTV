using System;
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

        private static Unit Seed(IServiceScope scope) =>
            Try(() => scope.ServiceProvider)
                .Bind(services => Try(GetDbContext(services)))
                .Bind(ctx => Try(Migrate(ctx)))
                .Bind(ctx => Try(InitializeDb(ctx)))
                .IfFail(ex => LogException(ex, scope.ServiceProvider));

        private static TvContext GetDbContext(IServiceProvider provider) =>
            provider.GetRequiredService<TvContext>();

        private static TvContext Migrate(TvContext context)
        {
            context.Database.Migrate();
            return context;
        }

        private static Unit InitializeDb(TvContext context) =>
            DbInitializer.Initialize(context);

        private static Unit LogException(Exception ex, IServiceProvider provider)
        {
            provider.GetRequiredService<ILogger<Program>>()
                .LogError(ex, "Error occured while seeding database");
            return unit;
        }
    }
}
