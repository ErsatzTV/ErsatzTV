using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class DeleteTemplateGroupHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<DeleteTemplateGroup, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(DeleteTemplateGroup request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<TemplateGroup> maybeTemplateGroup = await dbContext.TemplateGroups
            .SelectOneAsync(p => p.Id, p => p.Id == request.TemplateGroupId);

        foreach (TemplateGroup templateGroup in maybeTemplateGroup)
        {
            dbContext.TemplateGroups.Remove(templateGroup);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return maybeTemplateGroup.Match(
            _ => Option<BaseError>.None,
            () => BaseError.New($"TemplateGroup {request.TemplateGroupId} does not exist."));
    }
}
