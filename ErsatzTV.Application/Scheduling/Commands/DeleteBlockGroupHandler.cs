using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class DeleteBlockGroupHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<DeleteBlockGroup, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(DeleteBlockGroup request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<BlockGroup> maybeBlockGroup = await dbContext.BlockGroups
            .SelectOneAsync(p => p.Id, p => p.Id == request.BlockGroupId);

        foreach (BlockGroup blockGroup in maybeBlockGroup)
        {
            dbContext.BlockGroups.Remove(blockGroup);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return maybeBlockGroup.Match(
            _ => Option<BaseError>.None,
            () => BaseError.New($"BlockGroup {request.BlockGroupId} does not exist."));
    }
}
