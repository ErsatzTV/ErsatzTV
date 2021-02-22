using System.Collections.Generic;
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
            _dbContext.Channels.SingleOrDefaultAsync(c => c.Id == id).Map(Optional);

        public Task<Option<Channel>> GetByNumber(int number) =>
            _dbContext.Channels
                .Include(c => c.FFmpegProfile)
                .ThenInclude(p => p.Resolution)
                .SingleOrDefaultAsync(c => c.Number == number)
                .Map(Optional);

        public Task<List<Channel>> GetAll() => _dbContext.Channels.ToListAsync();

        public Task<List<Channel>> GetAllForGuide() =>
            _dbContext.Channels
                .Include(c => c.Playouts)
                .ThenInclude(p => p.Items)
                .ThenInclude(i => i.MediaItem)
                .ThenInclude(i => (i as TelevisionEpisodeMediaItem).Metadata)
                .Include(c => c.Playouts)
                .ThenInclude(p => p.Items)
                .ThenInclude(i => i.MediaItem)
                .ThenInclude(i => (i as MovieMediaItem).Metadata)
                .ToListAsync();

        public async Task Update(Channel channel)
        {
            _dbContext.Channels.Update(channel);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int channelId)
        {
            Channel channel = await _dbContext.Channels.FindAsync(channelId);
            _dbContext.Channels.Remove(channel);
            await _dbContext.SaveChangesAsync();
        }
    }
}
