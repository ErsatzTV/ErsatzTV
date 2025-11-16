using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels;

public class GetChannelByPlayoutIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetChannelByPlayoutId, Option<ChannelViewModel>>
{
    public async Task<Option<ChannelViewModel>> Handle(
        GetChannelByPlayoutId request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Playouts
            .Include(p => p.Channel)
            .ThenInclude(c => c.Artwork)
            .SingleOrDefaultAsync(p => p.Id == request.PlayoutId, cancellationToken)
            .Map(p => ProjectToViewModel(p.Channel, 1));
    }
}
