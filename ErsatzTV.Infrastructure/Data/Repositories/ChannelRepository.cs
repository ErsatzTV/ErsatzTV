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
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public ChannelRepository(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<Option<Channel>> Get(int id)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Channels
                .Include(c => c.Artwork)
                .Include(c => c.Watermark)
                .OrderBy(c => c.Id)
                .SingleOrDefaultAsync(c => c.Id == id)
                .Map(Optional);
        }

        public async Task<Option<Channel>> GetByNumber(string number)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Channels
                .Include(c => c.FFmpegProfile)
                .ThenInclude(p => p.Resolution)
                .Include(c => c.Artwork)
                .Include(c => c.Watermark)
                .OrderBy(c => c.Number)
                .SingleOrDefaultAsync(c => c.Number == number)
                .Map(Optional);
        }

        public async Task<List<Channel>> GetAll()
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Channels
                .Include(c => c.FFmpegProfile)
                .Include(c => c.Artwork)
                .ToListAsync();
        }

        public async Task<List<Channel>> GetAllForGuide()
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Channels
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
                .Include(c => c.Playouts)
                .ThenInclude(p => p.Items)
                .ThenInclude(i => i.MediaItem)
                .ThenInclude(i => (i as MusicVideo).MusicVideoMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(c => c.Playouts)
                .ThenInclude(p => p.Items)
                .ThenInclude(i => i.MediaItem)
                .ThenInclude(i => (i as MusicVideo).Artist)
                .ThenInclude(a => a.ArtistMetadata)
                .ToListAsync();
        }

        public async Task Delete(int channelId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Channel channel = await dbContext.Channels.FindAsync(channelId);
            dbContext.Channels.Remove(channel);
            await dbContext.SaveChangesAsync();
        }
    }
}
