using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class MovieMetadataHealthCheck : BaseHealthCheck, IMovieMetadataHealthCheck
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public MovieMetadataHealthCheck(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    protected override string Title => "Movie Metadata";
        
    public async Task<HealthCheckResult> Check()
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            
        List<Movie> movies = await dbContext.Movies
            .Filter(e => e.MovieMetadata.Count == 0)
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync();

        if (movies.Any())
        {
            var paths = movies.SelectMany(e => e.MediaVersions.Map(mv => mv.MediaFiles))
                .Flatten()
                .Bind(f => Optional<string>(Path.GetDirectoryName(f.Path)))
                .Distinct()
                .Take(5)
                .ToList();

            var folders = string.Join(", ", paths);

            return WarningResult($"There are {movies.Count} movies with missing metadata, including in the following folders: {folders}");
        }

        return OkResult();
    }
}