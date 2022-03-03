using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class EpisodeMetadataHealthCheck : BaseHealthCheck, IEpisodeMetadataHealthCheck
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public EpisodeMetadataHealthCheck(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    protected override string Title => "Episode Metadata";
        
    public async Task<HealthCheckResult> Check()
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            
        List<Episode> episodes = await dbContext.Episodes
            .Filter(e => e.EpisodeMetadata.Count == 0)
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync();

        if (episodes.Any())
        {
            var paths = episodes.SelectMany(e => e.MediaVersions.Map(mv => mv.MediaFiles))
                .Flatten()
                .Bind(f => Optional<string>(Path.GetDirectoryName(f.Path)))
                .Distinct()
                .Take(5)
                .ToList();

            var folders = string.Join(", ", paths);

            return WarningResult($"There are {episodes.Count} episodes with missing metadata, including in the following folders: {folders}");
        }

        return OkResult();
    }
}