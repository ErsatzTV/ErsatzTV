using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class GetPlayoutWarningsCountHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPlayoutWarningsCount, int>
{
    public async Task<int> Handle(GetPlayoutWarningsCount request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.PlayoutBuildStatus
            .CountAsync(bs => !bs.Success, cancellationToken);
    }
}
