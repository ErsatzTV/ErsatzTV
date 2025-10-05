using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetAllBlocksHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllBlocks, List<BlockViewModel>>
{
    public async Task<List<BlockViewModel>> Handle(GetAllBlocks request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Block> blocks = await dbContext.Blocks
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<BlockGroup> blockGroups = await dbContext.BlockGroups
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var unusedBlockGroups = blockGroups.ToList();

        // match blocks to block groups
        foreach (var block in blocks)
        {
            var maybeBlockGroup = blockGroups.FirstOrDefault(bg => bg.Id == block.BlockGroupId);
            if (maybeBlockGroup != null)
            {
                unusedBlockGroups.Remove(maybeBlockGroup);
                block.BlockGroup = maybeBlockGroup;
            }
        }

        // create dummy blocks for any groups that have no blocks yet
        foreach (var unusedGroup in unusedBlockGroups)
        {
            var dummyBlock = new Block
            {
                Id = unusedGroup.Id * -1,
                BlockGroupId = unusedGroup.Id,
                BlockGroup = unusedGroup,
                Name = "(none)"
            };

            blocks.Add(dummyBlock);
        }

        return blocks.Map(Mapper.ProjectToViewModel)
            .OrderBy(b => b.GroupName)
            .ThenBy(b => b.Name)
            .ToList();
    }
}
