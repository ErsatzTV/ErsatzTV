using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class DeleteDecoTemplateHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<DeleteDecoTemplate, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(DeleteDecoTemplate request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<DecoTemplate> maybeDecoTemplate = await dbContext.DecoTemplates
            .SelectOneAsync(p => p.Id, p => p.Id == request.DecoTemplateId);

        foreach (DecoTemplate decoTemplate in maybeDecoTemplate)
        {
            dbContext.DecoTemplates.Remove(decoTemplate);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return maybeDecoTemplate.Match(
            _ => Option<BaseError>.None,
            () => BaseError.New($"DecoTemplate {request.DecoTemplateId} does not exist."));
    }
}
