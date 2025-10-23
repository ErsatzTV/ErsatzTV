using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Services.RunOnce;

public class DatabaseCleanerService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DatabaseCleanerService> logger,
    SystemStartup systemStartup)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        await systemStartup.WaitForDatabase(stoppingToken);
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        logger.LogInformation("Cleaning database");

        using IServiceScope scope = serviceScopeFactory.CreateScope();
        await using TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

        await DeleteInvalidMediaItems(dbContext);
        await GenerateFallbackMetadata(scope, dbContext, stoppingToken);
        await ResetExternalSubtitleState(dbContext, stoppingToken);
        await RemoveOrphanedPlaylists(dbContext, stoppingToken);

        systemStartup.DatabaseIsCleaned();

        logger.LogInformation("Done cleaning database");
    }

    private static async Task DeleteInvalidMediaItems(TvContext dbContext) =>
        // some old version deleted items in a way that MediaItem was left over without
        // any corresponding Movie/Show/etc.
        // this cleans out that old invalid data
        await dbContext.Connection.ExecuteAsync(
            """
            delete
            from `MediaItem`
            where Id not in (select Id from `Movie`)
              and Id not in (select Id from `Show`)
              and Id not in (select Id from `Season`)
              and Id not in (select Id from `Episode`)
              and Id not in (select Id from `OtherVideo`)
              and Id not in (select Id from `MusicVideo`)
              and Id not in (select Id from `Song`)
              and Id not in (select Id from `Artist`)
              and Id not in (select Id from `Image`)
              and Id not in (select Id from `RemoteStream`)
            """);

    private static async Task GenerateFallbackMetadata(
        IServiceScope scope,
        TvContext dbContext,
        CancellationToken cancellationToken)
    {
        IFallbackMetadataProvider fallbackMetadataProvider =
            scope.ServiceProvider.GetRequiredService<IFallbackMetadataProvider>();

        List<Movie> movies = await dbContext.Movies
            .Filter(m => m.MovieMetadata.Count == 0)
            .Include(m => m.MovieMetadata)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync(cancellationToken);

        foreach (Movie movie in movies)
        {
            MovieMetadata metadata = fallbackMetadataProvider.GetFallbackMetadata(movie);
            metadata.SortTitle = SortTitle.GetSortTitle(metadata.Title);
            movie.MovieMetadata ??= [];
            movie.MovieMetadata.Add(metadata);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task ResetExternalSubtitleState(TvContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE `Subtitle` SET `IsExtracted`=0 WHERE `Path` IS NULL",
            cancellationToken);
    }

    private static async Task RemoveOrphanedPlaylists(TvContext dbContext, CancellationToken cancellationToken)
    {
        // remove orphaned playlists
        await dbContext.Playlists
            .Where(p => p.IsSystem && !dbContext.TraktLists.Any(t => t.PlaylistId == p.Id))
            .ExecuteDeleteAsync(cancellationToken);

        // turn off "generate playlist" on trakt lists with no referenced playlist
        await dbContext.TraktLists
            .Where(t => t.Playlist == null && t.GeneratePlaylist)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.GeneratePlaylist, false),
                cancellationToken);

        // set playback order when unset
        await dbContext.PlaylistItems
            .Where(pi => pi.Playlist.IsSystem && pi.PlaybackOrder == PlaybackOrder.None)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(p => p.PlaybackOrder, PlaybackOrder.Chronological),
                cancellationToken);
    }
}
