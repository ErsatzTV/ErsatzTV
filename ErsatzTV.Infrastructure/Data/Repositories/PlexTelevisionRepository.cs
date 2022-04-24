using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class PlexTelevisionRepository : IPlexTelevisionRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public PlexTelevisionRepository(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<PlexItemEtag>> GetExistingPlexShows(PlexLibrary library)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<PlexItemEtag>(
                @"SELECT PS.Key, PS.Etag, MI.State FROM PlexShow PS
                      INNER JOIN MediaItem MI on PS.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId",
                new { LibraryId = library.Id })
            .Map(result => result.ToList());
    }

    public async Task<List<PlexItemEtag>> GetExistingPlexSeasons(PlexLibrary library, PlexShow show)
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

    public async Task<List<PlexItemEtag>> GetExistingPlexEpisodes(PlexLibrary library, PlexSeason season)
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

    public async Task<Unit> SetPlexEtag(PlexShow show, string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE PlexShow SET Etag = @Etag WHERE Id = @Id",
            new { Etag = etag, show.Id }).Map(_ => Unit.Default);
    }

    public async Task<Unit> SetPlexEtag(PlexSeason season, string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE PlexSeason SET Etag = @Etag WHERE Id = @Id",
            new { Etag = etag, season.Id }).Map(_ => Unit.Default);
    }

    public async Task<Unit> SetPlexEtag(PlexEpisode episode, string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE PlexEpisode SET Etag = @Etag WHERE Id = @Id",
            new { Etag = etag, episode.Id }).Map(_ => Unit.Default);
    }
}
