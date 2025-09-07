using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public abstract class QuerySearchIndexHandlerBase
{
    protected static async Task<Option<JellyfinMediaSource>> GetJellyfin(
        TvContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.JellyfinMediaSources
            .AsNoTracking()
            .Include(p => p.Connections)
            .ToListAsync(cancellationToken)
            .Map(list => list.HeadOrNone());
    }

    protected static async Task<Option<EmbyMediaSource>> GetEmby(
        TvContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.EmbyMediaSources
            .AsNoTracking()
            .Include(p => p.Connections)
            .ToListAsync(cancellationToken)
            .Map(list => list.HeadOrNone());
    }
}
