using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetAllBlockGroupsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllBlockGroups, List<BlockGroupViewModel>>
{
    public async Task<List<BlockGroupViewModel>> Handle(GetAllBlockGroups request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<BlockGroup> blockGroups = await dbContext.BlockGroups
            .AsNoTracking()
            .Include(g => g.Blocks)
            .ToListAsync(cancellationToken);

        return blockGroups.Map(Mapper.ProjectToViewModel).ToList();
    }
}
