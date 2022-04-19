using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class FileNotFoundHealthCheck : BaseHealthCheck, IFileNotFoundHealthCheck
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public FileNotFoundHealthCheck(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    protected override string Title => "File Not Found";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Episode> episodes = await dbContext.Episodes
            .Filter(e => e.State == MediaItemState.FileNotFound)
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync(cancellationToken);

        List<Movie> movies = await dbContext.Movies
            .Filter(m => m.State == MediaItemState.FileNotFound)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync(cancellationToken);

        List<MusicVideo> musicVideos = await dbContext.MusicVideos
            .Filter(mv => mv.State == MediaItemState.FileNotFound)
            .Include(mv => mv.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync(cancellationToken);

        List<OtherVideo> otherVideos = await dbContext.OtherVideos
            .Filter(ov => ov.State == MediaItemState.FileNotFound)
            .Include(ov => ov.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync(cancellationToken);

        List<Song> songs = await dbContext.Songs
            .Filter(s => s.State == MediaItemState.FileNotFound)
            .Include(s => s.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync(cancellationToken);

        var all = movies.Map(m => m.MediaVersions.Head().MediaFiles.Head().Path)
            .Append(episodes.Map(e => e.MediaVersions.Head().MediaFiles.Head().Path))
            .Append(musicVideos.Map(mv => mv.GetHeadVersion().MediaFiles.Head().Path))
            .Append(otherVideos.Map(ov => ov.GetHeadVersion().MediaFiles.Head().Path))
            .Append(songs.Map(s => s.GetHeadVersion().MediaFiles.Head().Path))
            .ToList();

        if (all.Any())
        {
            var paths = all.Take(5).ToList();

            var files = string.Join(", ", paths);

            return WarningResult(
                $"There are {all.Count} files that do not exist on disk, including the following: {files}",
                "/media/trash");
        }

        return OkResult();
    }
}
