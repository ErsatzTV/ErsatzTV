using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class ChannelRepository : IChannelRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public ChannelRepository(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Option<Channel>> GetChannel(int id)
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
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Channels
            .Include(c => c.FFmpegProfile)
            .Include(c => c.Artwork)
            .Include(c => c.Playouts)
            .ToListAsync();
    }
}
