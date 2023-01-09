using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Channels;

public class GetChannelNameByPlayoutIdHandler : IRequestHandler<GetChannelNameByPlayoutId, Option<string>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetChannelNameByPlayoutIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Option<string>> Handle(GetChannelNameByPlayoutId request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Playouts
            .Include(p => p.Channel)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId)
            .MapT(p => p.Channel.Name);
    }
}
