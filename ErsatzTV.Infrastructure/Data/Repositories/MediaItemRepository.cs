using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MediaItemRepository : IMediaItemRepository
    {
        private readonly TvContext _dbContext;

        public MediaItemRepository(TvContext dbContext) => _dbContext = dbContext;

        public Task<Option<MediaItem>> Get(int id) =>
            _dbContext.MediaItems
                .Include(i => i.LibraryPath)
                .SingleOrDefaultAsync(i => i.Id == id)
                .Map(Optional);

        public Task<List<MediaItem>> GetAll() => _dbContext.MediaItems.ToListAsync();

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
            _dbContext.MediaItems.Update(mediaItem);
            return await _dbContext.SaveChangesAsync() > 0;
        }
    }
}
