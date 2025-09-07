using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class DeleteDecoGroupHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<DeleteDecoGroup, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(DeleteDecoGroup request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<DecoGroup> maybeDecoGroup = await dbContext.DecoGroups
            .SelectOneAsync(p => p.Id, p => p.Id == request.DecoGroupId, cancellationToken);

        foreach (DecoGroup decoGroup in maybeDecoGroup)
        {
            dbContext.DecoGroups.Remove(decoGroup);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return maybeDecoGroup.Match(
            _ => Option<BaseError>.None,
            () => BaseError.New($"DecoGroup {request.DecoGroupId} does not exist."));
    }
}
