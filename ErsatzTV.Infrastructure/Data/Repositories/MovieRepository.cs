using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using LanguageExt;
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
                .Include(i => i.LibraryPath)
                .ThenInclude(lp => lp.Library)
                .Include(i => i.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
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
                .ThenInclude(mm => mm.Artwork)
                .Include(i => i.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Include(i => i.LibraryPath)
                .ThenInclude(lp => lp.Library)
                .OrderBy(i => i.Key)
                .SingleOrDefaultAsync(i => i.Key == item.Key);

            return await maybeExisting.Match(
                plexMovie =>
                    Right<BaseError, MediaItemScanResult<PlexMovie>>(
                        new MediaItemScanResult<PlexMovie>(plexMovie) { IsAdded = true }).AsTask(),
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

        public async Task<List<int>> RemoveMissingPlexMovies(PlexLibrary library, List<string> movieKeys)
        {
            List<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT m.Id FROM MediaItem m
                INNER JOIN PlexMovie pm ON pm.Id = m.Id
                INNER JOIN LibraryPath lp ON lp.Id = m.LibraryPathId
                WHERE lp.LibraryId = @LibraryId AND pm.Key not in @Keys",
                new { LibraryId = library.Id, Keys = movieKeys }).Map(result => result.ToList());

            await _dbConnection.ExecuteAsync(@"DELETE FROM MediaItem WHERE Id IN @Ids", new { Ids = ids });

            return ids;
        }

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
                            }
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
                return BaseError.New(ex.Message);
            }
        }
    }
}
