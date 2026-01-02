using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class ChannelRepository(IDbContextFactory<TvContext> dbContextFactory) : IChannelRepository
{
    public async Task<Option<Channel>> GetChannel(int id)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.Artwork)
            .Include(c => c.Watermark)
            .OrderBy(c => c.Id)
            .SingleOrDefaultAsync(c => c.Id == id)
            .Map(Optional);
    }

    public async Task<Option<Channel>> GetByNumber(string number)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.FFmpegProfile)
            .ThenInclude(p => p.Resolution)
            .Include(c => c.Artwork)
            .Include(c => c.Watermark)
            .OrderBy(c => c.Number)
            .SingleOrDefaultAsync(c => c.Number == number)
            .Map(Optional);
    }

    public async Task<List<Channel>> GetAll(CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.FFmpegProfile)
            .Include(c => c.Artwork)
            .Include(c => c.Playouts)
            .Include(c => c.MirrorSourceChannel)
            .ThenInclude(mc => mc.Playouts)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<ChannelWatermark>> GetWatermarkByName(string name)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();

        List<ChannelWatermark> maybeWatermarks = await dbContext.ChannelWatermarks
            .AsNoTracking()
            .Where(cw => EF.Functions.Like(cw.Name, $"%{name}%"))
            .ToListAsync();

        return maybeWatermarks.HeadOrNone();
    }
}
