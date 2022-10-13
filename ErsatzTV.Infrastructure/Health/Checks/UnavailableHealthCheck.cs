using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class UnavailableHealthCheck : BaseHealthCheck, IUnavailableHealthCheck
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IEmbyPathReplacementService _embyPathReplacementService;
    private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
    private readonly IPlexPathReplacementService _plexPathReplacementService;

    public UnavailableHealthCheck(
        IDbContextFactory<TvContext> dbContextFactory,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService)
    {
        _dbContextFactory = dbContextFactory;
        _plexPathReplacementService = plexPathReplacementService;
        _jellyfinPathReplacementService = jellyfinPathReplacementService;
        _embyPathReplacementService = embyPathReplacementService;
    }

    public override string Title => "Unavailable";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<MediaItem> mediaItems = dbContext.MediaItems
            .Filter(mi => mi.State == MediaItemState.Unavailable)
            .Include(mi => (mi as Episode).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as Movie).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as OtherVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as Song).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as Show).ShowMetadata)
            .Include(mi => (mi as Season).Show)
            .ThenInclude(s => s.ShowMetadata)
            .Include(mi => (mi as Season).SeasonMetadata);

        List<MediaItem> five = await mediaItems
            .OrderBy(mi => mi.Id)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (mediaItems.Any())
        {
            var paths = new List<string>();

            foreach (MediaItem mediaItem in five)
            {
                string path = mediaItem switch
                {
                    Show s => s.ShowMetadata.Head().Title,
                    Season s => $"{s.Show.ShowMetadata.Head().Title} Season {s.SeasonNumber}",
                    _ => await mediaItem.GetLocalPath(
                        _plexPathReplacementService,
                        _jellyfinPathReplacementService,
                        _embyPathReplacementService,
                        false)
                };

                paths.Add(path);
            }

            var files = string.Join(", ", paths);

            int count = await mediaItems.CountAsync(cancellationToken);

            return WarningResult(
                $"There are {count} items that are unavailable because ErsatzTV cannot find them on disk, including the following: {files}",
                "search?query=state%3aUnavailable");
        }

        return OkResult();
    }
}
