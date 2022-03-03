using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class ZeroDurationHealthCheck : BaseHealthCheck, IZeroDurationHealthCheck
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public ZeroDurationHealthCheck(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    protected override string Title => "Zero Duration";

    public async Task<HealthCheckResult> Check()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<Episode> episodes = await dbContext.Episodes
            .Filter(e => e.MediaVersions.Any(mv => mv.Duration == TimeSpan.Zero))
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync();

        List<Movie> movies = await dbContext.Movies
            .Filter(m => m.MediaVersions.Any(mv => mv.Duration == TimeSpan.Zero))
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync();
            
        List<MusicVideo> musicVideos = await dbContext.MusicVideos
            .Filter(mv => mv.MediaVersions.Any(v => v.Duration == TimeSpan.Zero))
            .Include(mv => mv.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync();
            
        List<OtherVideo> otherVideos = await dbContext.OtherVideos
            .Filter(ov => ov.MediaVersions.Any(mv => mv.Duration == TimeSpan.Zero))
            .Include(ov => ov.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync();

        List<Song> songs = await dbContext.Songs
            .Filter(s => s.MediaVersions.Any(mv => mv.Duration == TimeSpan.Zero))
            .Include(s => s.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync();
            
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

            return WarningResult($"There are {all.Count} files with zero duration, including the following: {files}");
        }

        return OkResult();
    }
}