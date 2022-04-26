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

    public override string Title => "File Not Found";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<MediaItem> mediaItems = dbContext.MediaItems
            .Filter(mi => mi.State == MediaItemState.FileNotFound)
            .Include(mi => (mi as Episode).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as Movie).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as OtherVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as Song).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles);

        List<MediaItem> five = await mediaItems

            // shows and seasons don't have paths to display
            .Filter(mi => !(mi is Show))
            .Filter(mi => !(mi is Season))
            .OrderBy(mi => mi.Id)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (mediaItems.Any())
        {
            IEnumerable<string> paths = five.Map(mi => mi.GetHeadVersion().MediaFiles.Head().Path);

            var files = string.Join(", ", paths);

            int count = await mediaItems.CountAsync(cancellationToken);

            return WarningResult(
                $"There are {count} items that do not exist on disk, including the following: {files}",
                "/media/trash");
        }

        return OkResult();
    }
}
