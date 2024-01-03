using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Plex;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class PlexTelevisionRepository : IPlexTelevisionRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILogger<PlexTelevisionRepository> _logger;

    public PlexTelevisionRepository(
        IDbContextFactory<TvContext> dbContextFactory,
        ILogger<PlexTelevisionRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<Option<int>> FlagNormal(PlexLibrary library, PlexEpisode episode)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        episode.State = MediaItemState.Normal;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT PlexEpisode.Id FROM PlexEpisode
            INNER JOIN MediaItem MI ON MI.Id = PlexEpisode.Id
            INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
            WHERE PlexEpisode.Key = @Key",
            new { LibraryId = library.Id, episode.Key });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                @"UPDATE MediaItem SET State = 0 WHERE Id = @Id",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<Option<int>> FlagNormal(PlexLibrary library, PlexSeason season)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        season.State = MediaItemState.Normal;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT PlexSeason.Id FROM PlexSeason
            INNER JOIN MediaItem MI ON MI.Id = PlexSeason.Id
            INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
            WHERE PlexSeason.Key = @Key",
            new { LibraryId = library.Id, season.Key });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                @"UPDATE MediaItem SET State = 0 WHERE Id = @Id",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<Option<int>> FlagNormal(PlexLibrary library, PlexShow show)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        show.State = MediaItemState.Normal;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT PlexShow.Id FROM PlexShow
            INNER JOIN MediaItem MI ON MI.Id = PlexShow.Id
            INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
            WHERE PlexShow.Key = @Key",
            new { LibraryId = library.Id, show.Key });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                @"UPDATE MediaItem SET State = 0 WHERE Id = @Id",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<Option<int>> FlagUnavailable(PlexLibrary library, PlexEpisode episode)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        episode.State = MediaItemState.Unavailable;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT PlexEpisode.Id FROM PlexEpisode
            INNER JOIN MediaItem MI ON MI.Id = PlexEpisode.Id
            INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
            WHERE PlexEpisode.Key = @Key",
            new { LibraryId = library.Id, episode.Key });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                @"UPDATE MediaItem SET State = 2 WHERE Id = @Id",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<Option<int>> FlagRemoteOnly(PlexLibrary library, PlexEpisode episode)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        episode.State = MediaItemState.RemoteOnly;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT PlexEpisode.Id FROM PlexEpisode
            INNER JOIN MediaItem MI ON MI.Id = PlexEpisode.Id
            INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
            WHERE PlexEpisode.Key = @Key",
            new { LibraryId = library.Id, episode.Key });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                @"UPDATE MediaItem SET State = 3 WHERE Id = @Id",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<List<PlexItemEtag>> GetExistingShows(PlexLibrary library)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<PlexItemEtag>(
                @"SELECT PS.Key, PS.Etag, MI.State FROM PlexShow PS
                      INNER JOIN MediaItem MI on PS.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId",
                new { LibraryId = library.Id })
            .Map(result => result.ToList());
    }

    public async Task<List<PlexItemEtag>> GetExistingSeasons(PlexLibrary library, PlexShow show)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<PlexItemEtag>(
                @"SELECT PlexSeason.Key, PlexSeason.Etag, MI.State FROM PlexSeason
                      INNER JOIN Season S on PlexSeason.Id = S.Id
                      INNER JOIN MediaItem MI on S.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId
                      INNER JOIN PlexShow PS ON S.ShowId = PS.Id
                      WHERE LP.LibraryId = @LibraryId AND PS.Key = @Key",
                new { LibraryId = library.Id, show.Key })
            .Map(result => result.ToList());
    }

    public async Task<List<PlexItemEtag>> GetExistingEpisodes(PlexLibrary library, PlexSeason season)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<PlexItemEtag>(
                @"SELECT PlexEpisode.Key, PlexEpisode.Etag, MI.State FROM PlexEpisode
                      INNER JOIN Episode E on PlexEpisode.Id = E.Id
                      INNER JOIN MediaItem MI on E.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      INNER JOIN Season S2 on E.SeasonId = S2.Id
                      INNER JOIN PlexSeason PS on S2.Id = PS.Id
                      WHERE LP.LibraryId = @LibraryId AND PS.Key = @Key",
                new { LibraryId = library.Id, season.Key })
            .Map(result => result.ToList());
    }

    public async Task<Either<BaseError, MediaItemScanResult<PlexShow>>> GetOrAdd(PlexLibrary library, PlexShow item)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<PlexShow> maybeExisting = await dbContext.PlexShows
            .AsNoTracking()
            .Include(i => i.ShowMetadata)
            .ThenInclude(sm => sm.Genres)
            .Include(i => i.ShowMetadata)
            .ThenInclude(sm => sm.Tags)
            .Include(i => i.ShowMetadata)
            .ThenInclude(sm => sm.Studios)
            .Include(i => i.ShowMetadata)
            .ThenInclude(sm => sm.Actors)
            .ThenInclude(a => a.Artwork)
            .Include(i => i.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.ShowMetadata)
            .ThenInclude(sm => sm.Guids)
            .Include(i => i.LibraryPath)
            .ThenInclude(lp => lp.Library)
            .Include(i => i.TraktListItems)
            .ThenInclude(tli => tli.TraktList)
            .SelectOneAsync(i => i.Key, i => i.Key == item.Key);

        foreach (PlexShow plexShow in maybeExisting)
        {
            return new MediaItemScanResult<PlexShow>(plexShow) { IsAdded = false };
        }

        return await AddShow(dbContext, library, item);
    }

    public async Task<Either<BaseError, MediaItemScanResult<PlexSeason>>> GetOrAdd(PlexLibrary library, PlexSeason item)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<PlexSeason> maybeExisting = await dbContext.PlexSeasons
            .AsNoTracking()
            .Include(i => i.SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.SeasonMetadata)
            .ThenInclude(sm => sm.Guids)
            .Include(i => i.SeasonMetadata)
            .ThenInclude(sm => sm.Tags)
            .Include(s => s.LibraryPath)
            .ThenInclude(l => l.Library)
            .Include(s => s.TraktListItems)
            .ThenInclude(tli => tli.TraktList)
            .SelectOneAsync(i => i.Key, i => i.Key == item.Key);

        foreach (PlexSeason plexSeason in maybeExisting)
        {
            return new MediaItemScanResult<PlexSeason>(plexSeason) { IsAdded = false };
        }

        return await AddSeason(dbContext, library, item);
    }

    public async Task<Either<BaseError, MediaItemScanResult<PlexEpisode>>> GetOrAdd(
        PlexLibrary library,
        PlexEpisode item,
        bool deepScan)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<PlexEpisode> maybeExisting = await dbContext.PlexEpisodes
            .AsNoTracking()
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(mm => mm.Genres)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(mm => mm.Tags)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(mm => mm.Studios)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(mm => mm.Directors)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(mm => mm.Writers)
            .Include(i => i.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(e => e.EpisodeMetadata)
            .ThenInclude(em => em.Actors)
            .ThenInclude(a => a.Artwork)
            .Include(e => e.EpisodeMetadata)
            .ThenInclude(em => em.Guids)
            .Include(i => i.LibraryPath)
            .ThenInclude(lp => lp.Library)
            .Include(e => e.Season)
            .Include(e => e.TraktListItems)
            .ThenInclude(tli => tli.TraktList)
            .SelectOneAsync(i => i.Key, i => i.Key == item.Key);

        foreach (PlexEpisode plexEpisode in maybeExisting)
        {
            var result = new MediaItemScanResult<PlexEpisode>(plexEpisode) { IsAdded = false };

            // deepScan isn't needed here since we create our own plex etags
            if (plexEpisode.Etag != item.Etag)
            {
                foreach (BaseError error in await UpdateEpisodePath(dbContext, plexEpisode, item))
                {
                    return error;
                }

                result.IsUpdated = true;
            }

            return result;
        }

        return await AddEpisode(dbContext, library, item);
    }

    public async Task<Unit> SetEtag(PlexShow show, string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE PlexShow SET Etag = @Etag WHERE Id = @Id",
            new { Etag = etag, show.Id }).Map(_ => Unit.Default);
    }

    public async Task<Unit> SetEtag(PlexSeason season, string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE PlexSeason SET Etag = @Etag WHERE Id = @Id",
            new { Etag = etag, season.Id }).Map(_ => Unit.Default);
    }

    public async Task<Unit> SetEtag(PlexEpisode episode, string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE PlexEpisode SET Etag = @Etag WHERE Id = @Id",
            new { Etag = etag, episode.Id }).Map(_ => Unit.Default);
    }

    public async Task<List<int>> FlagFileNotFoundShows(PlexLibrary library, List<string> showItemIds)
    {
        if (showItemIds.Count == 0)
        {
            return new List<int>();
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MediaItem M
                INNER JOIN PlexShow ON PlexShow.Id = M.Id
                INNER JOIN LibraryPath LP on M.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId
                WHERE PlexShow.Key IN @ShowKeys",
                new { LibraryId = library.Id, ShowKeys = showItemIds })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 1 WHERE Id IN @Ids",
            new { Ids = ids });

        return ids;
    }

    public async Task<List<int>> FlagFileNotFoundSeasons(PlexLibrary library, List<string> seasonItemIds)
    {
        if (seasonItemIds.Count == 0)
        {
            return new List<int>();
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MediaItem M
                INNER JOIN PlexSeason ON PlexSeason.Id = M.Id
                INNER JOIN LibraryPath LP on M.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId
                WHERE PlexSeason.Key IN @SeasonKeys",
                new { LibraryId = library.Id, SeasonKeys = seasonItemIds })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 1 WHERE Id IN @Ids",
            new { Ids = ids });

        return ids;
    }

    public async Task<List<int>> FlagFileNotFoundEpisodes(PlexLibrary library, List<string> episodeItemIds)
    {
        if (episodeItemIds.Count == 0)
        {
            return new List<int>();
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MediaItem M
                INNER JOIN PlexEpisode ON PlexEpisode.Id = M.Id
                INNER JOIN LibraryPath LP on M.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId
                WHERE PlexEpisode.Key IN @EpisodeKeys",
                new { LibraryId = library.Id, EpisodeKeys = episodeItemIds })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 1 WHERE Id IN @Ids",
            new { Ids = ids });

        return ids;
    }

    private static async Task<Either<BaseError, MediaItemScanResult<PlexShow>>> AddShow(
        TvContext dbContext,
        PlexLibrary library,
        PlexShow item)
    {
        try
        {
            // blank out etag for initial save in case stats/metadata/etc updates fail
            string etag = item.Etag;
            item.Etag = string.Empty;

            item.LibraryPathId = library.Paths.Head().Id;

            await dbContext.PlexShows.AddAsync(item);
            await dbContext.SaveChangesAsync();

            // restore etag
            item.Etag = etag;

            await dbContext.Entry(item).Reference(i => i.LibraryPath).LoadAsync();
            await dbContext.Entry(item.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return new MediaItemScanResult<PlexShow>(item) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }

    private static async Task<Either<BaseError, MediaItemScanResult<PlexSeason>>> AddSeason(
        TvContext dbContext,
        PlexLibrary library,
        PlexSeason item)
    {
        try
        {
            // blank out etag for initial save in case stats/metadata/etc updates fail
            string etag = item.Etag;
            item.Etag = string.Empty;

            item.LibraryPathId = library.Paths.Head().Id;

            await dbContext.PlexSeasons.AddAsync(item);
            await dbContext.SaveChangesAsync();

            // restore etag
            item.Etag = etag;

            await dbContext.Entry(item).Reference(i => i.LibraryPath).LoadAsync();
            await dbContext.Entry(item.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return new MediaItemScanResult<PlexSeason>(item) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<PlexEpisode>>> AddEpisode(
        TvContext dbContext,
        PlexLibrary library,
        PlexEpisode item)
    {
        try
        {
            if (await MediaItemRepository.MediaFileAlreadyExists(item, library.Paths.Head().Id, dbContext, _logger))
            {
                return new MediaFileAlreadyExists();
            }

            // blank out etag for initial save in case stats/metadata/etc updates fail
            string etag = item.Etag;
            item.Etag = string.Empty;

            item.LibraryPathId = library.Paths.Head().Id;
            foreach (EpisodeMetadata metadata in item.EpisodeMetadata)
            {
                metadata.Genres ??= new List<Genre>();
                metadata.Tags ??= new List<Tag>();
                metadata.Studios ??= new List<Studio>();
                metadata.Actors ??= new List<Actor>();
                metadata.Directors ??= new List<Director>();
                metadata.Writers ??= new List<Writer>();
            }

            await dbContext.PlexEpisodes.AddAsync(item);
            await dbContext.SaveChangesAsync();

            // restore etag
            item.Etag = etag;

            await dbContext.Entry(item).Reference(i => i.LibraryPath).LoadAsync();
            await dbContext.Entry(item.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            await dbContext.Entry(item).Reference(e => e.Season).LoadAsync();
            return new MediaItemScanResult<PlexEpisode>(item) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }

    private async Task<Option<BaseError>> UpdateEpisodePath(
        TvContext dbContext,
        PlexEpisode existing,
        PlexEpisode incoming)
    {
        try
        {
            // library path is used for search indexing later
            incoming.LibraryPath = existing.LibraryPath;
            incoming.Id = existing.Id;

            // version
            MediaVersion version = existing.MediaVersions.Head();
            MediaVersion incomingVersion = incoming.MediaVersions.Head();
            version.Name = incomingVersion.Name;
            version.DateAdded = incomingVersion.DateAdded;

            // media file
            MediaFile file = version.MediaFiles.Head();
            MediaFile incomingFile = incomingVersion.MediaFiles.Head();

            _logger.LogDebug(
                "Updating plex episode (key {Key}) path from {Existing} to {Incoming}",
                existing.Key,
                file.Path,
                incomingFile.Path);

            file.Path = incomingFile.Path;

            await dbContext.Connection.ExecuteAsync(
                @"UPDATE MediaVersion SET Name = @Name, DateAdded = @DateAdded WHERE Id = @Id",
                new { version.Name, version.DateAdded, version.Id });

            await dbContext.Connection.ExecuteAsync(
                @"UPDATE MediaFile SET Path = @Path WHERE Id = @Id",
                new { file.Path, file.Id });

            return Option<BaseError>.None;
        }
        catch (Exception)
        {
            return BaseError.New("Failed to update episode path");
        }
    }
}
