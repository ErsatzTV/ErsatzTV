using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class PlexMovieRepository : IPlexMovieRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public PlexMovieRepository(IDbContextFactory<TvContext> dbContextFactory) => _dbContextFactory = dbContextFactory;

    public async Task<bool> FlagNormal(PlexLibrary library, PlexMovie movie)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        movie.State = MediaItemState.Normal;

        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 0 WHERE Id IN
            (SELECT PlexMovie.Id FROM PlexMovie
            INNER JOIN MediaItem MI ON MI.Id = PlexMovie.Id
            INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
            WHERE PlexMovie.Key = @Key)",
            new { LibraryId = library.Id, movie.Key }).Map(count => count > 0);
    }

    public async Task<bool> FlagUnavailable(PlexLibrary library, PlexMovie movie)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        movie.State = MediaItemState.Unavailable;

        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 2 WHERE Id IN
            (SELECT PlexMovie.Id FROM PlexMovie
            INNER JOIN MediaItem MI ON MI.Id = PlexMovie.Id
            INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
            WHERE PlexMovie.Key = @Key)",
            new { LibraryId = library.Id, movie.Key }).Map(count => count > 0);
    }

    public async Task<List<int>> FlagFileNotFound(PlexLibrary library, List<string> plexMovieKeys)
    {
        if (plexMovieKeys.Count == 0)
        {
            return new List<int>();
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MediaItem M
                INNER JOIN PlexMovie ON PlexMovie.Id = M.Id
                INNER JOIN LibraryPath LP on M.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId
                WHERE PlexMovie.Key IN @MovieKeys",
                new { LibraryId = library.Id, MovieKeys = plexMovieKeys })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 1 WHERE Id IN @Ids",
            new { Ids = ids });

        return ids;
    }
}
