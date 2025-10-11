using Dapper;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetAllBlocksForPlayoutHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllBlocksForPlayout, List<BlockViewModel>>
{
    public async Task<List<BlockViewModel>> Handle(GetAllBlocksForPlayout request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<int> blockIds = await dbContext.Connection.QueryAsync<int>(
                """
                SELECT DISTINCT TI.BlockId
                FROM TemplateItem TI
                INNER JOIN PlayoutTemplate PT ON PT.TemplateId = TI.TemplateId
                WHERE PT.PlayoutId = @PlayoutId
                """,
                new { request.PlayoutId })
            .Map(result => result.ToList());

        List<Block> blocks = await dbContext.Blocks
            .AsNoTracking()
            .Where(b => blockIds.Contains(b.Id))
            .ToListAsync(cancellationToken);

        List<BlockGroup> blockGroups = await dbContext.BlockGroups
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // match blocks to block groups
        foreach (var block in blocks)
        {
            var maybeBlockGroup = blockGroups.FirstOrDefault(bg => bg.Id == block.BlockGroupId);
            if (maybeBlockGroup != null)
            {
                block.BlockGroup = maybeBlockGroup;
            }
        }

        return blocks.Map(Mapper.ProjectToViewModel)
            .OrderBy(b => b.GroupName)
            .ThenBy(b => b.Name)
            .ToList();
    }
}
