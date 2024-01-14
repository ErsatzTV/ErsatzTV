using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class DeleteTemplateHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<DeleteTemplate, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(DeleteTemplate request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Template> maybeTemplate = await dbContext.Templates
            .SelectOneAsync(p => p.Id, p => p.Id == request.TemplateId);

        foreach (Template template in maybeTemplate)
        {
            dbContext.Templates.Remove(template);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return maybeTemplate.Match(
            _ => Option<BaseError>.None,
            () => BaseError.New($"Template {request.TemplateId} does not exist."));
    }
}
