using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Health.Checks
{
    public class ZeroDurationHealthCheck : BaseHealthCheck, IZeroDurationHealthCheck
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public ZeroDurationHealthCheck(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        protected override string Title => "Zero Duration";

        public async Task<HealthCheckResult> Check()
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            List<Episode> episodes = await dbContext.Episodes
                .Filter(e => e.MediaVersions.Any(mv => mv.Duration == TimeSpan.Zero))
                .Include(e => e.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .ToListAsync();

            List<Movie> movies = await dbContext.Movies
                .Filter(e => e.MediaVersions.Any(mv => mv.Duration == TimeSpan.Zero))
                .Include(e => e.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .ToListAsync();

            List<string> all = movies.Map(m => m.MediaVersions.Head().MediaFiles.Head().Path)
                .Append(episodes.Map(e => e.MediaVersions.Head().MediaFiles.Head().Path))
                .ToList();

            if (all.Any())
            {
                var paths = all.Take(5).ToList();

                var files = string.Join(", ", paths);

                return WarningResult($"There are {all.Count} files with zero duration, including the following: {files}");
            }

            return OkResult();
        }
    }
}
