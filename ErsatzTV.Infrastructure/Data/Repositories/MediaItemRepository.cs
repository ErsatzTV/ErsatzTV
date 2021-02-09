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

        public async Task<int> Add(MediaItem mediaItem)
        {
            await _dbContext.MediaItems.AddAsync(mediaItem);
            await _dbContext.SaveChangesAsync();
            return mediaItem.Id;
        }

        public Task<Option<MediaItem>> Get(int id) =>
            _dbContext.MediaItems.SingleOrDefaultAsync(i => i.Id == id).Map(Optional);

        public Task<List<MediaItem>> GetAll() => _dbContext.MediaItems.ToListAsync();

        public Task<List<MediaItem>> GetAll(MediaType mediaType) =>
            _dbContext.MediaItems
                .Include(i => i.Source)
                .Filter(i => i.Metadata.MediaType == mediaType)
                .ToListAsync();

        public Task<List<MediaItem>> GetAllByMediaSourceId(int mediaSourceId) =>
            _dbContext.MediaItems
                .Filter(i => i.MediaSourceId == mediaSourceId)
                .ToListAsync();

        public async Task Update(MediaItem mediaItem)
        {
            _dbContext.MediaItems.Update(mediaItem);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int mediaItemId)
        {
            MediaItem mediaItem = await _dbContext.MediaItems.FindAsync(mediaItemId);
            _dbContext.MediaItems.Remove(mediaItem);
            await _dbContext.SaveChangesAsync();
        }
    }
}
