using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class DeleteBlockHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<DeleteBlock, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(DeleteBlock request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Block> maybeBlock = await dbContext.Blocks
            .SelectOneAsync(p => p.Id, p => p.Id == request.BlockId);

        foreach (Block block in maybeBlock)
        {
            dbContext.Blocks.Remove(block);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return maybeBlock.Match(
            _ => Option<BaseError>.None,
            () => BaseError.New($"Block {request.BlockId} does not exist."));
    }
}
