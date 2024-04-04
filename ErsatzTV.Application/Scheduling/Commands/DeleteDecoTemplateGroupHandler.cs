using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class DeleteDecoTemplateGroupHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<DeleteDecoTemplateGroup, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(DeleteDecoTemplateGroup request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<DecoTemplateGroup> maybeDecoTemplateGroup = await dbContext.DecoTemplateGroups
            .SelectOneAsync(p => p.Id, p => p.Id == request.DecoTemplateGroupId);

        foreach (DecoTemplateGroup decoTemplateGroup in maybeDecoTemplateGroup)
        {
            dbContext.DecoTemplateGroups.Remove(decoTemplateGroup);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return maybeDecoTemplateGroup.Match(
            _ => Option<BaseError>.None,
            () => BaseError.New($"DecoTemplateGroup {request.DecoTemplateGroupId} does not exist."));
    }
}
