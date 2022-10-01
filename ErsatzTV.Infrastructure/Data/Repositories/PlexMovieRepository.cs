using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Plex;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class PlexMovieRepository : IPlexMovieRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public PlexMovieRepository(IDbContextFactory<TvContext> dbContextFactory) => _dbContextFactory = dbContextFactory;

    public async Task<List<PlexItemEtag>> GetExistingMovies(PlexLibrary library)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<PlexItemEtag>(
                @"SELECT Key, Etag, MI.State FROM PlexMovie
                      INNER JOIN Movie M on PlexMovie.Id = M.Id
                      INNER JOIN MediaItem MI on M.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      WHERE LP.LibraryId = @LibraryId",
                new { LibraryId = library.Id })
            .Map(result => result.ToList());
    }

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

    public async Task<Option<int>> FlagUnavailable(PlexLibrary library, PlexMovie movie)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        movie.State = MediaItemState.Unavailable;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT PlexMovie.Id FROM PlexMovie
              INNER JOIN MediaItem MI ON MI.Id = PlexMovie.Id
              INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
              WHERE PlexMovie.Key = @Key",
            new { LibraryId = library.Id, movie.Key });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                @"UPDATE MediaItem SET State = 2 WHERE Id = @Id",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
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

    public async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> GetOrAdd(PlexLibrary library, PlexMovie item)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<PlexMovie> maybeExisting = await dbContext.PlexMovies
            .AsNoTracking()
            .Include(i => i.MovieMetadata)
            .ThenInclude(mm => mm.Genres)
            .Include(i => i.MovieMetadata)
            .ThenInclude(mm => mm.Tags)
            .Include(i => i.MovieMetadata)
            .ThenInclude(mm => mm.Studios)
            .Include(i => i.MovieMetadata)
            .ThenInclude(mm => mm.Actors)
            .ThenInclude(a => a.Artwork)
            .Include(i => i.MovieMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(i => i.MovieMetadata)
            .ThenInclude(mm => mm.Directors)
            .Include(i => i.MovieMetadata)
            .ThenInclude(mm => mm.Writers)
            .Include(i => i.MovieMetadata)
            .ThenInclude(mm => mm.Guids)
            .Include(i => i.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.LibraryPath)
            .ThenInclude(lp => lp.Library)
            .Include(i => i.TraktListItems)
            .ThenInclude(tli => tli.TraktList)
            .SelectOneAsync(i => i.Key, i => i.Key == item.Key);

        foreach (PlexMovie plexMovie in maybeExisting)
        {
            var result = new MediaItemScanResult<PlexMovie>(plexMovie) { IsAdded = false };
            if (plexMovie.Etag != item.Etag)
            {
                await UpdateMoviePath(dbContext, plexMovie, item);
                result.IsUpdated = true;
            }

            return result;
        }

        return await AddMovie(dbContext, library, item);
    }

    public async Task<Unit> SetEtag(PlexMovie movie, string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE PlexMovie SET Etag = @Etag WHERE Id = @Id",
            new { Etag = etag, movie.Id }).Map(_ => Unit.Default);
    }

    private async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> AddMovie(
        TvContext dbContext,
        PlexLibrary library,
        PlexMovie item)
    {
        try
        {
            // blank out etag for initial save in case stats/metadata/etc updates fail
            string etag = item.Etag;
            item.Etag = string.Empty;

            item.LibraryPathId = library.Paths.Head().Id;

            await dbContext.PlexMovies.AddAsync(item);
            await dbContext.SaveChangesAsync();

            // restore etag
            item.Etag = etag;

            await dbContext.Entry(item).Reference(i => i.LibraryPath).LoadAsync();
            await dbContext.Entry(item.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return new MediaItemScanResult<PlexMovie>(item) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }
    
    private static async Task UpdateMoviePath(TvContext dbContext, PlexMovie existing, PlexMovie incoming)
    {
        // library path is used for search indexing later
        incoming.LibraryPath = existing.LibraryPath;
        incoming.Id = existing.Id;

        // version
        MediaVersion version = existing.MediaVersions.Head();
        MediaVersion incomingVersion = incoming.MediaVersions.Head();
        version.Name = incomingVersion.Name;
        version.DateAdded = incomingVersion.DateAdded;

        await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaVersion SET Name = @Name, DateAdded = @DateAdded WHERE Id = @Id",
            new { version.Name, version.DateAdded, version.Id });

        // media file
        MediaFile file = version.MediaFiles.Head();
        MediaFile incomingFile = incomingVersion.MediaFiles.Head();
        file.Path = incomingFile.Path;

        await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaFile SET Path = @Path WHERE Id = @Id",
            new { file.Path, file.Id });
    }
}
