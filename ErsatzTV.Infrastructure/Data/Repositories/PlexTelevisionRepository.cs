using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Plex;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class PlexTelevisionRepository : IPlexTelevisionRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public PlexTelevisionRepository(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<bool> FlagNormal(PlexLibrary library, PlexEpisode episode)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        episode.State = MediaItemState.Normal;

        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 0 WHERE Id IN
            (SELECT PlexEpisode.Id FROM PlexEpisode
            INNER JOIN MediaItem MI ON MI.Id = PlexEpisode.Id
            INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
            WHERE PlexEpisode.Key = @Key)",
            new { LibraryId = library.Id, episode.Key }).Map(count => count > 0);
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
        PlexEpisode item)
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
            return new MediaItemScanResult<PlexEpisode>(plexEpisode) { IsAdded = false };
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

    public async Task<List<int>> FlagFileNotFoundShows(PlexLibrary library, List<string> plexShowKeys)
    {
        if (plexShowKeys.Count == 0)
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
                new { LibraryId = library.Id, ShowKeys = plexShowKeys })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 1 WHERE Id IN @Ids",
            new { Ids = ids });

        return ids;
    }

    public async Task<List<int>> FlagFileNotFoundSeasons(PlexLibrary library, List<string> plexSeasonKeys)
    {
        if (plexSeasonKeys.Count == 0)
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
                new { LibraryId = library.Id, SeasonKeys = plexSeasonKeys })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 1 WHERE Id IN @Ids",
            new { Ids = ids });

        return ids;
    }

    public async Task<List<int>> FlagFileNotFoundEpisodes(PlexLibrary library, List<string> plexEpisodeKeys)
    {
        if (plexEpisodeKeys.Count == 0)
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
                new { LibraryId = library.Id, EpisodeKeys = plexEpisodeKeys })
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
            item.Etag = string.Empty;

            item.LibraryPathId = library.Paths.Head().Id;

            await dbContext.PlexShows.AddAsync(item);
            await dbContext.SaveChangesAsync();

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
            item.Etag = string.Empty;

            item.LibraryPathId = library.Paths.Head().Id;

            await dbContext.PlexSeasons.AddAsync(item);
            await dbContext.SaveChangesAsync();

            await dbContext.Entry(item).Reference(i => i.LibraryPath).LoadAsync();
            await dbContext.Entry(item.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return new MediaItemScanResult<PlexSeason>(item) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }

    private static async Task<Either<BaseError, MediaItemScanResult<PlexEpisode>>> AddEpisode(
        TvContext dbContext,
        PlexLibrary library,
        PlexEpisode item)
    {
        try
        {
            if (dbContext.MediaFiles.Any(mf => mf.Path == item.MediaVersions.Head().MediaFiles.Head().Path))
            {
                return BaseError.New("Multi-episode files are not yet supported");
            }

            // blank out etag for initial save in case stats/metadata/etc updates fail
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
}
