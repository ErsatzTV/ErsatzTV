using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Channels;

public class GetChannelNameByPlayoutIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetChannelNameByPlayoutId, Option<string>>
{
    public async Task<Option<string>> Handle(GetChannelNameByPlayoutId request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Playouts
            .Include(p => p.Channel)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId, cancellationToken)
            .MapT(p => p.Channel.Name);
    }
}
