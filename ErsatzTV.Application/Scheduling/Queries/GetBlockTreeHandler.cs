using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetBlockTreeHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetBlockTree, BlockTreeViewModel>
{
    public async Task<BlockTreeViewModel> Handle(GetBlockTree request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<BlockGroup> blockGroups = await dbContext.BlockGroups
            .AsNoTracking()
            .Include(g => g.Blocks)
            .ToListAsync(cancellationToken);

        return Mapper.ProjectToViewModel(blockGroups);
    }
}
