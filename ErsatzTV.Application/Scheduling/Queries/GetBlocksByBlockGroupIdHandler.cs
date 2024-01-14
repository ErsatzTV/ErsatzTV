using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetBlocksByBlockGroupIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetBlocksByBlockGroupId, List<BlockViewModel>>
{
    public async Task<List<BlockViewModel>> Handle(GetBlocksByBlockGroupId request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Block> blocks = await dbContext.Blocks
            .Filter(b => b.BlockGroupId == request.BlockGroupId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return blocks.Map(Mapper.ProjectToViewModel).ToList();
    }
}
