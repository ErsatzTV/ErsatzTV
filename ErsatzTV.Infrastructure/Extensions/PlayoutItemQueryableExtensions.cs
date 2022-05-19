using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Extensions;

public static class PlayoutItemQueryableExtensions
{
    public static Task<Option<PlayoutItem>> ForChannelAndTime(
        this IQueryable<PlayoutItem> dbSet,
        int channelId,
        DateTimeOffset time) =>
        dbSet.Filter(pi => pi.Playout.ChannelId == channelId)
            .Filter(pi => pi.Start <= time.UtcDateTime && pi.Finish > time.UtcDateTime)
            .OrderBy(pi => pi.Start)
            .FirstOrDefaultAsync()
            .Map(Optional);
}
