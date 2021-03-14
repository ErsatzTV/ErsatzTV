using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MediaItemRepository : IMediaItemRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public MediaItemRepository(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public async Task<Option<MediaItem>> Get(int id)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            return await context.MediaItems
                .Include(i => i.LibraryPath)
                .OrderBy(i => i.Id)
                .SingleOrDefaultAsync(i => i.Id == id)
                .Map(Optional);
        }

        public async Task<List<MediaItem>> GetAll()
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            return await context.MediaItems.ToListAsync();
        }

        public Task<List<MediaItem>> Search(string searchString) =>
            // TODO: fix this when we need to search
            // IQueryable<TelevisionEpisodeMediaItem> episodeData =
            //     from c in _dbContext.TelevisionEpisodeMediaItems.Include(c => c.LibraryPath) select c;
            //
            // if (!string.IsNullOrEmpty(searchString))
            // {
            //     episodeData = episodeData.Where(c => EF.Functions.Like(c.Metadata.Title, $"%{searchString}%"));
            // }
            //
            // IQueryable<Movie> movieData =
            //     from c in _dbContext.Movies.Include(c => c.LibraryPath) select c;
            //
            // // if (!string.IsNullOrEmpty(searchString))
            // // {
            // //     movieData = movieData.Where(c => EF.Functions.Like(c.Metadata.Title, $"%{searchString}%"));
            // // }
            //
            // return episodeData.OfType<MediaItem>().Concat(movieData.OfType<MediaItem>()).ToListAsync();
            new List<MediaItem>().AsTask();

        public async Task<bool> Update(MediaItem mediaItem)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            context.MediaItems.Update(mediaItem);
            return await context.SaveChangesAsync() > 0;
        }
    }
}
