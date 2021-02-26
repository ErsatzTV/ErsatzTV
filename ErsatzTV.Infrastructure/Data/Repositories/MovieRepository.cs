using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly TvContext _dbContext;

        public MovieRepository(TvContext dbContext) => _dbContext = dbContext;

        public Task<Option<Movie>> GetMovie(int movieId) =>
            _dbContext.Movies
                .Include(m => m.Metadata)
                .SingleOrDefaultAsync(m => m.Id == movieId)
                .Map(Optional);

        // TODO: fix this - need to add to library path, not media source
        public async Task<Either<BaseError, Movie>> GetOrAdd(LibraryPath libraryPath, string path)
        {
            Option<Movie> maybeExisting = await _dbContext.Movies
                .Include(i => i.Metadata)
                .Include(i => i.LibraryPath)
                .SingleOrDefaultAsync(i => i.Path == path);

            return await maybeExisting.Match(
                mediaItem => Right<BaseError, Movie>(mediaItem).AsTask(),
                async () => await AddMovie(libraryPath.Id, path));
        }

        public async Task<Either<BaseError, PlexMovie>> GetOrAdd(
            PlexLibrary library,
            PlexMovie item)
        {
            Option<PlexMovie> maybeExisting = await _dbContext.PlexMovieMediaItems
                .Include(i => i.Metadata)
                .Include(i => i.Part)
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
            _dbContext.Movies
                .AsNoTracking()
                .CountAsync();

        public Task<List<Movie>> GetPagedMovies(int pageNumber, int pageSize) =>
            _dbContext.Movies
                .Include(s => s.MovieMetadata)
                .OrderBy(s => s.MovieMetadata.FirstOrDefault().SortTitle)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

        private async Task<Either<BaseError, Movie>> AddMovie(int libraryPathId, string path)
        {
            try
            {
                var movie = new Movie { LibraryPathId = libraryPathId, Path = path };
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

        private async Task<Either<BaseError, PlexMovie>> AddPlexMovie(
            PlexLibrary library,
            PlexMovie item)
        {
            try
            {
                // TODO: this should be library path id
                item.LibraryPathId = library.Paths.Head().Id;

                await _dbContext.PlexMovieMediaItems.AddAsync(item);
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
