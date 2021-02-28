using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MovieRepository : IMovieRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly TvContext _dbContext;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public MovieRepository(
            TvContext dbContext,
            IDbContextFactory<TvContext> dbContextFactory,
            IDbConnection dbConnection)
        {
            _dbContext = dbContext;
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public async Task<Option<Movie>> GetMovie(int movieId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Movies
                .Include(m => m.MovieMetadata)
                .ThenInclude(m => m.Artwork)
                .OrderBy(m => m.Id)
                .SingleOrDefaultAsync(m => m.Id == movieId)
                .Map(Optional);
        }

        public async Task<Either<BaseError, Movie>> GetOrAdd(LibraryPath libraryPath, string path)
        {
            Option<Movie> maybeExisting = await _dbContext.Movies
                .Include(i => i.MovieMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(i => i.LibraryPath)
                .Include(i => i.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .OrderBy(i => i.MediaVersions.First().MediaFiles.First().Path)
                .SingleOrDefaultAsync(i => i.MediaVersions.First().MediaFiles.First().Path == path);

            return await maybeExisting.Match(
                mediaItem => Right<BaseError, Movie>(mediaItem).AsTask(),
                async () => await AddMovie(libraryPath.Id, path));
        }

        public async Task<Either<BaseError, PlexMovie>> GetOrAdd(PlexLibrary library, PlexMovie item)
        {
            Option<PlexMovie> maybeExisting = await _dbContext.PlexMovies
                .Include(i => i.MovieMetadata)
                .Include(i => i.MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .OrderBy(i => i.Key)
                .SingleOrDefaultAsync(i => i.Key == item.Key);

            return await maybeExisting.Match(
                plexMovie => Right<BaseError, PlexMovie>(plexMovie).AsTask(),
                async () => await AddPlexMovie(library, item));
        }

        public async Task<bool> Update(Movie movie)
        {
            _dbContext.Movies.Update(movie);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public Task<int> GetMovieCount() =>
            _dbConnection.QuerySingleAsync<int>(@"SELECT COUNT(DISTINCT MovieId) FROM MovieMetadata");

        public Task<List<MovieMetadata>> GetPagedMovies(int pageNumber, int pageSize) =>
            _dbContext.MovieMetadata.FromSqlRaw(
                    @"SELECT * FROM MovieMetadata WHERE Id IN
            (SELECT Id FROM MovieMetadata GROUP BY MovieId, MetadataKind HAVING MetadataKind = MAX(MetadataKind))
            ORDER BY SortTitle
            LIMIT {0} OFFSET {1}",
                    pageSize,
                    (pageNumber - 1) * pageSize)
                .Include(mm => mm.Artwork)
                .OrderBy(mm => mm.SortTitle)
                .ToListAsync();

        private async Task<Either<BaseError, Movie>> AddMovie(int libraryPathId, string path)
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
                await _dbContext.Movies.AddAsync(movie);
                await _dbContext.SaveChangesAsync();
                await _dbContext.Entry(movie).Reference(m => m.LibraryPath).LoadAsync();
                return movie;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, PlexMovie>> AddPlexMovie(PlexLibrary library, PlexMovie item)
        {
            try
            {
                item.LibraryPathId = library.Paths.Head().Id;

                await _dbContext.PlexMovies.AddAsync(item);
                await _dbContext.SaveChangesAsync();
                await _dbContext.Entry(item).Reference(i => i.LibraryPath).LoadAsync();
                return item;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }
    }
}
