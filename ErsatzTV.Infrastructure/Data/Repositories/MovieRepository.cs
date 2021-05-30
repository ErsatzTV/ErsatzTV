using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Core.Metadata;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MovieRepository : IMovieRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public MovieRepository(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public Task<bool> AllMoviesExist(List<int> movieIds) =>
            _dbConnection.QuerySingleAsync<int>(
                    "SELECT COUNT(*) FROM Movie WHERE Id in @MovieIds",
                    new { MovieIds = movieIds })
                .Map(c => c == movieIds.Count);

        public async Task<Option<Movie>> GetMovie(int movieId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Movies
                .Include(m => m.MovieMetadata)
                .ThenInclude(m => m.Artwork)
                .Include(m => m.MovieMetadata)
                .ThenInclude(m => m.Genres)
                .Include(m => m.MovieMetadata)
                .ThenInclude(m => m.Tags)
                .Include(m => m.MovieMetadata)
                .ThenInclude(m => m.Studios)
                .Include(m => m.MovieMetadata)
                .ThenInclude(m => m.Actors)
                .ThenInclude(a => a.Artwork)
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Directors)
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Writers)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.Streams)
                .OrderBy(m => m.Id)
                .SingleOrDefaultAsync(m => m.Id == movieId)
                .Map(Optional);
        }

        public async Task<Either<BaseError, MediaItemScanResult<Movie>>> GetOrAdd(LibraryPath libraryPath, string path)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<Movie> maybeExisting = await dbContext.Movies
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Genres)
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Tags)
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Studios)
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Guids)
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Actors)
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Actors)
                .ThenInclude(a => a.Artwork)
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Directors)
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Writers)
                .Include(i => i.LibraryPath)
                .ThenInclude(lp => lp.Library)
                .Include(i => i.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Include(i => i.MediaVersions)
                .ThenInclude(mv => mv.Streams)
                .OrderBy(i => i.MediaVersions.First().MediaFiles.First().Path)
                .SingleOrDefaultAsync(i => i.MediaVersions.First().MediaFiles.First().Path == path);

            return await maybeExisting.Match(
                mediaItem =>
                    Right<BaseError, MediaItemScanResult<Movie>>(
                        new MediaItemScanResult<Movie>(mediaItem) { IsAdded = false }).AsTask(),
                async () => await AddMovie(dbContext, libraryPath.Id, path));
        }

        public async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> GetOrAdd(
            PlexLibrary library,
            PlexMovie item)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            Option<PlexMovie> maybeExisting = await context.PlexMovies
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
                .OrderBy(i => i.Key)
                .SingleOrDefaultAsync(i => i.Key == item.Key);

            return await maybeExisting.Match(
                plexMovie =>
                    Right<BaseError, MediaItemScanResult<PlexMovie>>(
                        new MediaItemScanResult<PlexMovie>(plexMovie) { IsAdded = false }).AsTask(),
                async () => await AddPlexMovie(context, library, item));
        }

        public Task<int> GetMovieCount() =>
            _dbConnection.QuerySingleAsync<int>(@"SELECT COUNT(DISTINCT MovieId) FROM MovieMetadata");

        public async Task<List<MovieMetadata>> GetPagedMovies(int pageNumber, int pageSize)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.MovieMetadata.FromSqlRaw(
                    @"SELECT * FROM MovieMetadata WHERE Id IN
            (SELECT Id FROM MovieMetadata GROUP BY MovieId, MetadataKind HAVING MetadataKind = MAX(MetadataKind))
            ORDER BY SortTitle
            LIMIT {0} OFFSET {1}",
                    pageSize,
                    (pageNumber - 1) * pageSize)
                .AsNoTracking()
                .Include(mm => mm.Artwork)
                .OrderBy(mm => mm.SortTitle)
                .ToListAsync();
        }

        public async Task<List<MovieMetadata>> GetMoviesForCards(List<int> ids)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.MovieMetadata
                .AsNoTracking()
                .Filter(mm => ids.Contains(mm.MovieId))
                .Include(mm => mm.Artwork)
                .OrderBy(mm => mm.SortTitle)
                .ToListAsync();
        }

        public Task<IEnumerable<string>> FindMoviePaths(LibraryPath libraryPath) =>
            _dbConnection.QueryAsync<string>(
                @"SELECT MF.Path
                FROM MediaFile MF
                INNER JOIN MediaVersion MV on MF.MediaVersionId = MV.Id
                INNER JOIN Movie M on MV.MovieId = M.Id
                INNER JOIN MediaItem MI on M.Id = MI.Id
                WHERE MI.LibraryPathId = @LibraryPathId",
                new { LibraryPathId = libraryPath.Id });

        public async Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            List<int> ids = await _dbConnection.QueryAsync<int>(
                    @"SELECT M.Id
                FROM Movie M
                INNER JOIN MediaItem MI on M.Id = MI.Id
                INNER JOIN MediaVersion MV on M.Id = MV.MovieId
                INNER JOIN MediaFile MF on MV.Id = MF.MediaVersionId
                WHERE MI.LibraryPathId = @LibraryPathId AND MF.Path = @Path",
                    new { LibraryPathId = libraryPath.Id, Path = path })
                .Map(result => result.ToList());

            foreach (int movieId in ids)
            {
                Movie movie = await dbContext.Movies.FindAsync(movieId);
                dbContext.Movies.Remove(movie);
            }

            bool changed = await dbContext.SaveChangesAsync() > 0;
            return changed ? ids : new List<int>();
        }

        public Task<bool> AddGenre(MovieMetadata metadata, Genre genre) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Genre (Name, MovieMetadataId) VALUES (@Name, @MetadataId)",
                new { genre.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public Task<bool> AddTag(MovieMetadata metadata, Tag tag) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Tag (Name, MovieMetadataId) VALUES (@Name, @MetadataId)",
                new { tag.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public Task<bool> AddStudio(MovieMetadata metadata, Studio studio) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Studio (Name, MovieMetadataId) VALUES (@Name, @MetadataId)",
                new { studio.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public async Task<bool> AddActor(MovieMetadata metadata, Actor actor)
        {
            int? artworkId = null;

            if (actor.Artwork != null)
            {
                artworkId = await _dbConnection.QuerySingleAsync<int>(
                    @"INSERT INTO Artwork (ArtworkKind, DateAdded, DateUpdated, Path)
                      VALUES (@ArtworkKind, @DateAdded, @DateUpdated, @Path);
                      SELECT last_insert_rowid()",
                    new
                    {
                        ArtworkKind = (int) actor.Artwork.ArtworkKind,
                        actor.Artwork.DateAdded,
                        actor.Artwork.DateUpdated,
                        actor.Artwork.Path
                    });
            }

            return await _dbConnection.ExecuteAsync(
                    "INSERT INTO Actor (Name, Role, \"Order\", MovieMetadataId, ArtworkId) VALUES (@Name, @Role, @Order, @MetadataId, @ArtworkId)",
                    new { actor.Name, actor.Role, actor.Order, MetadataId = metadata.Id, ArtworkId = artworkId })
                .Map(result => result > 0);
        }

        public async Task<List<int>> RemoveMissingPlexMovies(PlexLibrary library, List<string> movieKeys)
        {
            List<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN PlexMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                WHERE lp.LibraryId = @LibraryId AND pm.Key not in @Keys",
                new { LibraryId = library.Id, Keys = movieKeys }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT m.Id FROM MediaItem m
                INNER JOIN PlexMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                WHERE lp.LibraryId = @LibraryId AND pm.Key not in @Keys)",
                new { LibraryId = library.Id, Keys = movieKeys });

            return ids;
        }

        public Task<bool> UpdateSortTitle(MovieMetadata movieMetadata) =>
            _dbConnection.ExecuteAsync(
                @"UPDATE MovieMetadata SET SortTitle = @SortTitle WHERE Id = @Id",
                new { movieMetadata.SortTitle, movieMetadata.Id }).Map(result => result > 0);

        public Task<List<JellyfinItemEtag>> GetExistingJellyfinMovies(JellyfinLibrary library) =>
            _dbConnection.QueryAsync<JellyfinItemEtag>(
                    @"SELECT ItemId, Etag FROM JellyfinMovie
                      INNER JOIN Movie M on JellyfinMovie.Id = M.Id
                      INNER JOIN MediaItem MI on M.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      WHERE LP.LibraryId = @LibraryId",
                    new { LibraryId = library.Id })
                .Map(result => result.ToList());

        public async Task<List<int>> RemoveMissingJellyfinMovies(JellyfinLibrary library, List<string> movieIds)
        {
            List<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT JellyfinMovie.Id FROM JellyfinMovie
                  INNER JOIN Movie M on JellyfinMovie.Id = M.Id
                  INNER JOIN MediaItem MI on M.Id = MI.Id
                  INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                  WHERE LP.LibraryId = @LibraryId AND ItemId IN @ItemIds",
                new { LibraryId = library.Id, ItemIds = movieIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                "DELETE FROM JellyfinMovie WHERE Id IN @Ids",
                new { Ids = ids });

            return ids;
        }

        public async Task<bool> AddJellyfin(JellyfinMovie movie)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            await dbContext.AddAsync(movie);
            if (await dbContext.SaveChangesAsync() <= 0)
            {
                return false;
            }

            await dbContext.Entry(movie).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(movie.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return true;
        }

        public async Task<Option<JellyfinMovie>> UpdateJellyfin(JellyfinMovie movie)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<JellyfinMovie> maybeExisting = await dbContext.JellyfinMovies
                .Include(m => m.LibraryPath)
                .ThenInclude(lp => lp.Library)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.Streams)
                .Include(m => m.MovieMetadata)
                .ThenInclude(mm => mm.Genres)
                .Include(m => m.MovieMetadata)
                .ThenInclude(mm => mm.Tags)
                .Include(m => m.MovieMetadata)
                .ThenInclude(mm => mm.Studios)
                .Include(m => m.MovieMetadata)
                .ThenInclude(mm => mm.Actors)
                .Include(m => m.MovieMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(m => m.MovieMetadata)
                .ThenInclude(mm => mm.Directors)
                .Include(m => m.MovieMetadata)
                .ThenInclude(mm => mm.Writers)
                .Include(m => m.MovieMetadata)
                .ThenInclude(mm => mm.Guids)
                .Filter(m => m.ItemId == movie.ItemId)
                .OrderBy(m => m.ItemId)
                .SingleOrDefaultAsync();

            if (maybeExisting.IsSome)
            {
                JellyfinMovie existing = maybeExisting.ValueUnsafe();

                // library path is used for search indexing later
                movie.LibraryPath = existing.LibraryPath;
                movie.Id = existing.Id;

                existing.Etag = movie.Etag;

                // metadata
                MovieMetadata metadata = existing.MovieMetadata.Head();
                MovieMetadata incomingMetadata = movie.MovieMetadata.Head();
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
                MediaVersion incomingVersion = movie.MediaVersions.Head();
                version.Name = incomingVersion.Name;
                version.DateAdded = incomingVersion.DateAdded;

                // media file
                MediaFile file = version.MediaFiles.Head();
                MediaFile incomingFile = incomingVersion.MediaFiles.Head();
                file.Path = incomingFile.Path;
            }

            await dbContext.SaveChangesAsync();

            return maybeExisting;
        }

        public Task<List<EmbyItemEtag>> GetExistingEmbyMovies(EmbyLibrary library) =>
            _dbConnection.QueryAsync<EmbyItemEtag>(
                    @"SELECT ItemId, Etag FROM EmbyMovie
                      INNER JOIN Movie M on EmbyMovie.Id = M.Id
                      INNER JOIN MediaItem MI on M.Id = MI.Id
                      INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                      WHERE LP.LibraryId = @LibraryId",
                    new { LibraryId = library.Id })
                .Map(result => result.ToList());

        public async Task<List<int>> RemoveMissingEmbyMovies(EmbyLibrary library, List<string> movieIds)
        {
            List<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT EmbyMovie.Id FROM EmbyMovie
                  INNER JOIN Movie M on EmbyMovie.Id = M.Id
                  INNER JOIN MediaItem MI on M.Id = MI.Id
                  INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                  WHERE LP.LibraryId = @LibraryId AND ItemId IN @ItemIds",
                new { LibraryId = library.Id, ItemIds = movieIds }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(
                "DELETE FROM EmbyMovie WHERE Id IN @Ids",
                new { Ids = ids });

            return ids;
        }

        public async Task<bool> AddEmby(EmbyMovie movie)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            await dbContext.AddAsync(movie);
            if (await dbContext.SaveChangesAsync() <= 0)
            {
                return false;
            }

            await dbContext.Entry(movie).Reference(m => m.LibraryPath).LoadAsync();
            await dbContext.Entry(movie.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            return true;
        }

        public async Task<Option<EmbyMovie>> UpdateEmby(EmbyMovie movie)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<EmbyMovie> maybeExisting = await dbContext.EmbyMovies
                .Include(m => m.LibraryPath)
                .ThenInclude(lp => lp.Library)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Include(m => m.MediaVersions)
                .ThenInclude(mv => mv.Streams)
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
                .Filter(m => m.ItemId == movie.ItemId)
                .OrderBy(m => m.ItemId)
                .SingleOrDefaultAsync();

            if (maybeExisting.IsSome)
            {
                EmbyMovie existing = maybeExisting.ValueUnsafe();

                // library path is used for search indexing later
                movie.LibraryPath = existing.LibraryPath;
                movie.Id = existing.Id;

                existing.Etag = movie.Etag;

                // metadata
                MovieMetadata metadata = existing.MovieMetadata.Head();
                MovieMetadata incomingMetadata = movie.MovieMetadata.Head();
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
                MediaVersion incomingVersion = movie.MediaVersions.Head();
                version.Name = incomingVersion.Name;
                version.DateAdded = incomingVersion.DateAdded;

                // media file
                MediaFile file = version.MediaFiles.Head();
                MediaFile incomingFile = incomingVersion.MediaFiles.Head();
                file.Path = incomingFile.Path;
            }

            await dbContext.SaveChangesAsync();

            return maybeExisting;
        }

        public Task<bool> AddDirector(MovieMetadata metadata, Director director) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Director (Name, MovieMetadataId) VALUES (@Name, @MetadataId)",
                new { director.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        public Task<bool> AddWriter(MovieMetadata metadata, Writer writer) =>
            _dbConnection.ExecuteAsync(
                "INSERT INTO Writer (Name, MovieMetadataId) VALUES (@Name, @MetadataId)",
                new { writer.Name, MetadataId = metadata.Id }).Map(result => result > 0);

        private static async Task<Either<BaseError, MediaItemScanResult<Movie>>> AddMovie(
            TvContext dbContext,
            int libraryPathId,
            string path)
        {
            try
            {
                var movie = new Movie
                {
                    LibraryPathId = libraryPathId,
                    MediaVersions = new List<MediaVersion>
                    {
                        new()
                        {
                            MediaFiles = new List<MediaFile>
                            {
                                new() { Path = path }
                            },
                            Streams = new List<MediaStream>()
                        }
                    }
                };
                await dbContext.Movies.AddAsync(movie);
                await dbContext.SaveChangesAsync();
                await dbContext.Entry(movie).Reference(m => m.LibraryPath).LoadAsync();
                await dbContext.Entry(movie.LibraryPath).Reference(lp => lp.Library).LoadAsync();
                return new MediaItemScanResult<Movie>(movie) { IsAdded = true };
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> AddPlexMovie(
            TvContext context,
            PlexLibrary library,
            PlexMovie item)
        {
            try
            {
                item.LibraryPathId = library.Paths.Head().Id;

                await context.PlexMovies.AddAsync(item);
                await context.SaveChangesAsync();
                await context.Entry(item).Reference(i => i.LibraryPath).LoadAsync();
                await context.Entry(item.LibraryPath).Reference(lp => lp.Library).LoadAsync();
                return new MediaItemScanResult<PlexMovie>(item) { IsAdded = true };
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.ToString());
            }
        }
    }
}
