﻿using Dapper;
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

public class PlexMovieRepository : IPlexMovieRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILogger<PlexMovieRepository> _logger;

    public PlexMovieRepository(IDbContextFactory<TvContext> dbContextFactory, ILogger<PlexMovieRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<List<PlexItemEtag>> GetExistingMovies(PlexLibrary library)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<PlexItemEtag>(
                @"SELECT `Key`, Etag, MI.State FROM PlexMovie
                      INNER JOIN Movie M on PlexMovie.Id = M.Id
                      INNER JOIN MediaItem MI on M.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      WHERE LP.LibraryId = @LibraryId",
                new { LibraryId = library.Id })
            .Map(result => result.ToList());
    }

    public async Task<Option<int>> FlagNormal(PlexLibrary library, PlexMovie movie)
    {
        if (movie.State is MediaItemState.Normal)
        {
            return Option<int>.None;
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        movie.State = MediaItemState.Normal;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT PlexMovie.Id FROM PlexMovie
              INNER JOIN MediaItem MI ON MI.Id = PlexMovie.Id
              INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
              WHERE PlexMovie.Key = @Key",
            new { LibraryId = library.Id, movie.Key });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                "UPDATE MediaItem SET State = 0 WHERE Id = @Id AND State != 0",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<Option<int>> FlagUnavailable(PlexLibrary library, PlexMovie movie)
    {
        if (movie.State is MediaItemState.Unavailable)
        {
            return Option<int>.None;
        }

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
                "UPDATE MediaItem SET State = 2 WHERE Id = @Id AND State != 2",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<Option<int>> FlagRemoteOnly(PlexLibrary library, PlexMovie movie)
    {
        if (movie.State is MediaItemState.RemoteOnly)
        {
            return Option<int>.None;
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        movie.State = MediaItemState.RemoteOnly;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT PlexMovie.Id FROM PlexMovie
              INNER JOIN MediaItem MI ON MI.Id = PlexMovie.Id
              INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
              WHERE PlexMovie.Key = @Key",
            new { LibraryId = library.Id, movie.Key });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                "UPDATE MediaItem SET State = 3 WHERE Id = @Id AND State != 3",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<List<int>> FlagFileNotFound(PlexLibrary library, List<string> movieItemIds)
    {
        if (movieItemIds.Count == 0)
        {
            return [];
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MediaItem M
                INNER JOIN PlexMovie ON PlexMovie.Id = M.Id
                INNER JOIN LibraryPath LP on M.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId
                WHERE PlexMovie.Key IN @MovieKeys",
                new { LibraryId = library.Id, MovieKeys = movieItemIds })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            "UPDATE MediaItem SET State = 1 WHERE Id IN @Ids AND State != 1",
            new { Ids = ids });

        return ids;
    }

    public async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> GetOrAdd(
        PlexLibrary library,
        PlexMovie item,
        bool deepScan)
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
            if (plexMovie.Etag != item.Etag || deepScan)
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
            if (await MediaItemRepository.MediaFileAlreadyExists(item, library.Paths.Head().Id, dbContext, _logger))
            {
                return new MediaFileAlreadyExists();
            }

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
        if (version.MediaFiles.Head() is PlexMediaFile file &&
            incomingVersion.MediaFiles.Head() is PlexMediaFile incomingFile)
        {
            file.Path = incomingFile.Path;
            file.Key = incomingFile.Key;

            await dbContext.Connection.ExecuteAsync(
                @"UPDATE MediaFile SET Path = @Path WHERE Id = @Id",
                new { file.Path, file.Id });

            await dbContext.Connection.ExecuteAsync(
                @"UPDATE PlexMediaFile SET `Key` = @Key WHERE Id = @Id",
                new { file.Key, file.Id });
        }
    }
}
