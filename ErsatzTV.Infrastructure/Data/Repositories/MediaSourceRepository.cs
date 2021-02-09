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
    public class MediaSourceRepository : IMediaSourceRepository
    {
        private readonly TvContext _dbContext;

        public MediaSourceRepository(TvContext dbContext) => _dbContext = dbContext;

        public async Task<LocalMediaSource> Add(LocalMediaSource localMediaSource)
        {
            await _dbContext.LocalMediaSources.AddAsync(localMediaSource);
            await _dbContext.SaveChangesAsync();
            return localMediaSource;
        }

        public async Task<PlexMediaSource> Add(PlexMediaSource plexMediaSource)
        {
            await _dbContext.PlexMediaSources.AddAsync(plexMediaSource);
            await _dbContext.SaveChangesAsync();
            return plexMediaSource;
        }

        public async Task<List<MediaSource>> GetAll()
        {
            List<MediaSource> all = await _dbContext.MediaSources.ToListAsync();
            foreach (PlexMediaSource plex in all.OfType<PlexMediaSource>())
            {
                await _dbContext.Entry(plex).Collection(p => p.Connections).LoadAsync();
            }

            return all;
        }

        public Task<List<PlexMediaSource>> GetAllPlex() =>
            _dbContext.PlexMediaSources
                .Include(p => p.Connections)
                .ToListAsync();

        public Task<Option<MediaSource>> Get(int id) =>
            _dbContext.MediaSources
                .SingleOrDefaultAsync(s => s.Id == id)
                .Map(Optional);

        public Task<Option<PlexMediaSource>> GetPlex(int id) =>
            _dbContext.PlexMediaSources
                .Include(p => p.Connections)
                .Include(p => p.Libraries)
                .SingleOrDefaultAsync(p => p.Id == id)
                .Map(Optional);

        public Task<int> CountMediaItems(int id) =>
            _dbContext.MediaItems.CountAsync(i => i.MediaSourceId == id);

        public async Task Update(PlexMediaSource plexMediaSource)
        {
            _dbContext.PlexMediaSources.Update(plexMediaSource);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            MediaSource mediaSource = await _dbContext.MediaSources.FindAsync(id);
            _dbContext.MediaSources.Remove(mediaSource);
            await _dbContext.SaveChangesAsync();
        }
    }
}
