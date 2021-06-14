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
    public class ChannelRepository : IChannelRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public ChannelRepository(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

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

        public async Task<bool> Update(Channel channel)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            dbContext.Entry(channel).State = EntityState.Modified;
            if (channel.Watermark != null)
            {
                dbContext.Entry(channel.Watermark).State =
                    channel.WatermarkId == null ? EntityState.Added : EntityState.Modified;
            }

            foreach (Artwork artwork in Optional(channel.Artwork).Flatten())
            {
                dbContext.Entry(artwork).State = artwork.Id > 0 ? EntityState.Modified : EntityState.Added;
            }

            bool result = await dbContext.SaveChangesAsync() > 0;
            return result;
        }

        public async Task Delete(int channelId)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Channel channel = await dbContext.Channels.FindAsync(channelId);
            dbContext.Channels.Remove(channel);
            await dbContext.SaveChangesAsync();
        }

        public async Task<Unit> RemoveWatermark(Channel channel)
        {
            if (channel.Watermark != null)
            {
                await _dbConnection.ExecuteAsync(
                    "UPDATE Channel SET WatermarkId = NULL WHERE Id = @ChannelId",
                    new { ChannelId = channel.Id });

                await _dbConnection.ExecuteAsync(
                    "DELETE FROM ChannelWatermark WHERE Id = @WatermarkId",
                    new { channel.WatermarkId });

                channel.Watermark = null;
                channel.WatermarkId = null;
            }

            return Unit.Default;
        }
    }
}
