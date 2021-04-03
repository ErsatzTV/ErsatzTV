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
    public class ChannelRepository : IChannelRepository
    {
        private readonly TvContext _dbContext;

        public ChannelRepository(TvContext dbContext) => _dbContext = dbContext;

        public async Task<Channel> Add(Channel channel)
        {
            await _dbContext.Channels.AddAsync(channel);
            await _dbContext.SaveChangesAsync();
            return channel;
        }

        public Task<Option<Channel>> Get(int id) =>
            _dbContext.Channels
                .Include(c => c.Artwork)
                .OrderBy(c => c.Id)
                .SingleOrDefaultAsync(c => c.Id == id)
                .Map(Optional);

        public Task<Option<Channel>> GetByNumber(string number) =>
            _dbContext.Channels
                .Include(c => c.FFmpegProfile)
                .ThenInclude(p => p.Resolution)
                .OrderBy(c => c.Number)
                .SingleOrDefaultAsync(c => c.Number == number)
                .Map(Optional);

        public Task<List<Channel>> GetAll() =>
            _dbContext.Channels
                .Include(c => c.FFmpegProfile)
                .Include(c => c.Artwork)
                .ToListAsync();

        public Task<List<Channel>> GetAllForGuide() =>
            _dbContext.Channels
                .Include(c => c.Artwork)
                .Include(c => c.Playouts)
                .ThenInclude(p => p.Items)
                .ThenInclude(i => i.MediaItem)
                .ThenInclude(i => (i as Episode).EpisodeMetadata)
                .Include(c => c.Playouts)
                .ThenInclude(p => p.Items)
                .ThenInclude(i => i.MediaItem)
                .ThenInclude(i => (i as Episode).Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(c => c.Playouts)
                .ThenInclude(p => p.Items)
                .ThenInclude(i => i.MediaItem)
                .ThenInclude(i => (i as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Artwork)
                .ToListAsync();

        public Task Update(Channel channel)
        {
            _dbContext.Channels.Update(channel);
            return _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int channelId)
        {
            Channel channel = await _dbContext.Channels.FindAsync(channelId);
            _dbContext.Channels.Remove(channel);
            await _dbContext.SaveChangesAsync();
        }
    }
}
