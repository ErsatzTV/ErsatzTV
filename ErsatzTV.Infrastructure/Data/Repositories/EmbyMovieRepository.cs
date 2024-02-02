﻿using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class EmbyMovieRepository : IEmbyMovieRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILogger<EmbyMovieRepository> _logger;

    public EmbyMovieRepository(IDbContextFactory<TvContext> dbContextFactory, ILogger<EmbyMovieRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<List<EmbyItemEtag>> GetExistingMovies(EmbyLibrary library)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<EmbyItemEtag>(
                @"SELECT ItemId, Etag, MI.State FROM EmbyMovie
                      INNER JOIN Movie M on EmbyMovie.Id = M.Id
                      INNER JOIN MediaItem MI on M.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      WHERE LP.LibraryId = @LibraryId",
                new { LibraryId = library.Id })
            .Map(result => result.ToList());
    }

    public async Task<Option<int>> FlagNormal(EmbyLibrary library, EmbyMovie movie)
    {
        if (movie.State is MediaItemState.Normal)
        {
            return Option<int>.None;
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        movie.State = MediaItemState.Normal;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT EmbyMovie.Id FROM EmbyMovie
              INNER JOIN MediaItem MI ON MI.Id = EmbyMovie.Id
              INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
              WHERE EmbyMovie.ItemId = @ItemId",
            new { LibraryId = library.Id, movie.ItemId });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                "UPDATE MediaItem SET State = 0 WHERE Id = @Id AND State != 0",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<Option<int>> FlagUnavailable(EmbyLibrary library, EmbyMovie movie)
    {
        if (movie.State is MediaItemState.Unavailable)
        {
            return Option<int>.None;
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        movie.State = MediaItemState.Unavailable;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT EmbyMovie.Id FROM EmbyMovie
              INNER JOIN MediaItem MI ON MI.Id = EmbyMovie.Id
              INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
              WHERE EmbyMovie.ItemId = @ItemId",
            new { LibraryId = library.Id, movie.ItemId });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                "UPDATE MediaItem SET State = 2 WHERE Id = @Id AND State != 2",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<Option<int>> FlagRemoteOnly(EmbyLibrary library, EmbyMovie movie)
    {
        if (movie.State is MediaItemState.RemoteOnly)
        {
            return Option<int>.None;
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        movie.State = MediaItemState.RemoteOnly;

        Option<int> maybeId = await dbContext.Connection.ExecuteScalarAsync<int>(
            @"SELECT EmbyMovie.Id FROM EmbyMovie
              INNER JOIN MediaItem MI ON MI.Id = EmbyMovie.Id
              INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id AND LibraryId = @LibraryId
              WHERE EmbyMovie.ItemId = @ItemId",
            new { LibraryId = library.Id, movie.ItemId });

        foreach (int id in maybeId)
        {
            return await dbContext.Connection.ExecuteAsync(
                "UPDATE MediaItem SET State = 3 WHERE Id = @Id AND State != 3",
                new { Id = id }).Map(count => count > 0 ? Some(id) : None);
        }

        return None;
    }

    public async Task<List<int>> FlagFileNotFound(EmbyLibrary library, List<string> movieItemIds)
    {
        if (movieItemIds.Count == 0)
        {
            return [];
        }

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MediaItem M
                INNER JOIN EmbyMovie ON EmbyMovie.Id = M.Id
                INNER JOIN LibraryPath LP on M.LibraryPathId = LP.Id AND LP.LibraryId = @LibraryId
                WHERE EmbyMovie.ItemId IN @MovieItemIds",
                new { LibraryId = library.Id, MovieItemIds = movieItemIds })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            "UPDATE MediaItem SET State = 1 WHERE Id IN @Ids AND State != 1",
            new { Ids = ids });

        return ids;
    }

    public async Task<Either<BaseError, MediaItemScanResult<EmbyMovie>>> GetOrAdd(
        EmbyLibrary library,
        EmbyMovie item,
        bool deepScan)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<EmbyMovie> maybeExisting = await dbContext.EmbyMovies
            .Include(m => m.LibraryPath)
            .ThenInclude(lp => lp.Library)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Genres)
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Tags)
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Studios)
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Actors)
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Directors)
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Writers)
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Artwork)
            .Include(m => m.MovieMetadata)
            .ThenInclude(mm => mm.Guids)
            .Include(m => m.TraktListItems)
            .ThenInclude(tli => tli.TraktList)
            .SelectOneAsync(m => m.ItemId, m => m.ItemId == item.ItemId);

        foreach (EmbyMovie embyMovie in maybeExisting)
        {
            var result = new MediaItemScanResult<EmbyMovie>(embyMovie) { IsAdded = false };
            if (embyMovie.Etag != item.Etag || deepScan)
            {
                await UpdateMovie(dbContext, embyMovie, item);
                result.IsUpdated = true;
            }

            return result;
        }

        return await AddMovie(dbContext, library, item);
    }

    public async Task<Unit> SetEtag(EmbyMovie movie, string etag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE EmbyMovie SET Etag = @Etag WHERE Id = @Id",
            new { Etag = etag, movie.Id }).Map(_ => Unit.Default);
    }

    private async Task<Either<BaseError, MediaItemScanResult<EmbyMovie>>> AddMovie(
        TvContext dbContext,
        EmbyLibrary library,
        EmbyMovie movie)
    {
        try
        {
            if (await MediaItemRepository.MediaFileAlreadyExists(movie, library.Paths.Head().Id, dbContext, _logger))
            {
                return new MediaFileAlreadyExists();
            }

            // blank out etag for initial save in case other updates fail
            string etag = movie.Etag;
            movie.Etag = string.Empty;

            movie.LibraryPathId = library.Paths.Head().Id;

            await dbContext.AddAsync(movie);
            await dbContext.SaveChangesAsync();

            // restore etag
            movie.Etag = etag;

            await dbContext.Entry(movie).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(movie.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return new MediaItemScanResult<EmbyMovie>(movie) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }

    private static async Task UpdateMovie(TvContext dbContext, EmbyMovie existing, EmbyMovie incoming)
    {
        // library path is used for search indexing later
        incoming.LibraryPath = existing.LibraryPath;
        incoming.Id = existing.Id;

        // metadata
        MovieMetadata metadata = existing.MovieMetadata.Head();
        MovieMetadata incomingMetadata = incoming.MovieMetadata.Head();
        metadata.MetadataKind = incomingMetadata.MetadataKind;
        metadata.ContentRating = incomingMetadata.ContentRating;
        metadata.Title = incomingMetadata.Title;
        metadata.SortTitle = incomingMetadata.SortTitle;
        metadata.Plot = incomingMetadata.Plot;
        metadata.Year = incomingMetadata.Year;
        metadata.Tagline = incomingMetadata.Tagline;
        metadata.DateAdded = incomingMetadata.DateAdded;
        metadata.DateUpdated = DateTime.UtcNow;

        // genres
        foreach (Genre genre in metadata.Genres
                     .Filter(g => incomingMetadata.Genres.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Genres.Remove(genre);
        }

        foreach (Genre genre in incomingMetadata.Genres
                     .Filter(g => metadata.Genres.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Genres.Add(genre);
        }

        // tags
        foreach (Tag tag in metadata.Tags
                     .Filter(g => incomingMetadata.Tags.All(g2 => g2.Name != g.Name))
                     .Filter(g => g.ExternalCollectionId is null)
                     .ToList())
        {
            metadata.Tags.Remove(tag);
        }

        foreach (Tag tag in incomingMetadata.Tags
                     .Filter(g => metadata.Tags.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Tags.Add(tag);
        }

        // studios
        foreach (Studio studio in metadata.Studios
                     .Filter(g => incomingMetadata.Studios.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Studios.Remove(studio);
        }

        foreach (Studio studio in incomingMetadata.Studios
                     .Filter(g => metadata.Studios.All(g2 => g2.Name != g.Name))
                     .ToList())
        {
            metadata.Studios.Add(studio);
        }

        // actors
        foreach (Actor actor in metadata.Actors
                     .Filter(
                         a => incomingMetadata.Actors.All(
                             a2 => a2.Name != a.Name || a.Artwork == null && a2.Artwork != null))
                     .ToList())
        {
            metadata.Actors.Remove(actor);
        }

        foreach (Actor actor in incomingMetadata.Actors
                     .Filter(a => metadata.Actors.All(a2 => a2.Name != a.Name))
                     .ToList())
        {
            metadata.Actors.Add(actor);
        }

        // directors
        foreach (Director director in metadata.Directors
                     .Filter(d => incomingMetadata.Directors.All(d2 => d2.Name != d.Name))
                     .ToList())
        {
            metadata.Directors.Remove(director);
        }

        foreach (Director director in incomingMetadata.Directors
                     .Filter(d => metadata.Directors.All(d2 => d2.Name != d.Name))
                     .ToList())
        {
            metadata.Directors.Add(director);
        }

        // writers
        foreach (Writer writer in metadata.Writers
                     .Filter(w => incomingMetadata.Writers.All(w2 => w2.Name != w.Name))
                     .ToList())
        {
            metadata.Writers.Remove(writer);
        }

        foreach (Writer writer in incomingMetadata.Writers
                     .Filter(w => metadata.Writers.All(w2 => w2.Name != w.Name))
                     .ToList())
        {
            metadata.Writers.Add(writer);
        }

        // guids
        foreach (MetadataGuid guid in metadata.Guids
                     .Filter(g => incomingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            metadata.Guids.Remove(guid);
        }

        foreach (MetadataGuid guid in incomingMetadata.Guids
                     .Filter(g => metadata.Guids.All(g2 => g2.Guid != g.Guid))
                     .ToList())
        {
            metadata.Guids.Add(guid);
        }

        metadata.ReleaseDate = incomingMetadata.ReleaseDate;

        // poster
        Artwork incomingPoster =
            incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster);
        if (incomingPoster != null)
        {
            Artwork poster = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster);
            if (poster == null)
            {
                poster = new Artwork { ArtworkKind = ArtworkKind.Poster };
                metadata.Artwork.Add(poster);
            }

            poster.Path = incomingPoster.Path;
            poster.DateAdded = incomingPoster.DateAdded;
            poster.DateUpdated = incomingPoster.DateUpdated;
        }

        // fan art
        Artwork incomingFanArt =
            incomingMetadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.FanArt);
        if (incomingFanArt != null)
        {
            Artwork fanArt = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.FanArt);
            if (fanArt == null)
            {
                fanArt = new Artwork { ArtworkKind = ArtworkKind.FanArt };
                metadata.Artwork.Add(fanArt);
            }

            fanArt.Path = incomingFanArt.Path;
            fanArt.DateAdded = incomingFanArt.DateAdded;
            fanArt.DateUpdated = incomingFanArt.DateUpdated;
        }

        // version
        MediaVersion version = existing.MediaVersions.Head();
        MediaVersion incomingVersion = incoming.MediaVersions.Head();
        version.Name = incomingVersion.Name;
        version.DateAdded = incomingVersion.DateAdded;
        version.Chapters = incomingVersion.Chapters;

        // media file
        MediaFile file = version.MediaFiles.Head();
        MediaFile incomingFile = incomingVersion.MediaFiles.Head();
        file.Path = incomingFile.Path;

        await dbContext.SaveChangesAsync();
    }
}
