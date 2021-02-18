using System;
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

        public async Task<Either<BaseError, MovieMediaItem>> GetOrAdd(int mediaSourceId, string path)
        {
            Option<MovieMediaItem> maybeExisting = await _dbContext.MovieMediaItems
                .Include(i => i.Metadata)
                .SingleOrDefaultAsync(i => i.Path == path);

            return await maybeExisting.Match(
                mediaItem => Right<BaseError, MovieMediaItem>(mediaItem).AsTask(),
                async () => await AddMovie(mediaSourceId, path));
        }

        private async Task<Either<BaseError, MovieMediaItem>> AddMovie(int mediaSourceId, string path)
        {
            try
            {
                var movie = new MovieMediaItem { MediaSourceId = mediaSourceId, Path = path };
                await _dbContext.MovieMediaItems.AddAsync(movie);
                await _dbContext.SaveChangesAsync();
                return movie;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }
    }
}
