using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class GetPlayoutIdByChannelNumberHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPlayoutIdByChannelNumber, Option<int>>
{
    public async Task<Option<int>> Handle(GetPlayoutIdByChannelNumber request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Playouts
            .Filter(p => p.Channel.Number == request.ChannelNumber)
            .Map(p => p.Id)
            .ToListAsync(cancellationToken)
            .Map(list => list.HeadOrNone());
    }
}
