using System.Collections.Generic;
using System.Linq;
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
                .Include(i => i.Source)
                .SingleOrDefaultAsync(i => i.Id == id)
                .Map(Optional);

        public Task<List<MediaItem>> GetAll() => _dbContext.MediaItems.ToListAsync();

        public Task<List<MediaItem>> Search(string searchString)
        {
            IQueryable<TelevisionEpisodeMediaItem> episodeData =
                from c in _dbContext.TelevisionEpisodeMediaItems.Include(c => c.Source) select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                episodeData = episodeData.Where(c => EF.Functions.Like(c.Metadata.Title, $"%{searchString}%"));
            }

            IQueryable<MovieMediaItem> movieData =
                from c in _dbContext.MovieMediaItems.Include(c => c.Source) select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                movieData = movieData.Where(c => EF.Functions.Like(c.Metadata.Title, $"%{searchString}%"));
            }

            return episodeData.OfType<MediaItem>().Concat(movieData.OfType<MediaItem>()).ToListAsync();
        }

        public async Task<bool> Update(MediaItem mediaItem)
        {
            _dbContext.MediaItems.Update(mediaItem);
            return await _dbContext.SaveChangesAsync() > 0;
        }
    }
}
