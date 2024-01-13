using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetTemplatesByTemplateGroupIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetTemplatesByTemplateGroupId, List<TemplateViewModel>>
{
    public async Task<List<TemplateViewModel>> Handle(
        GetTemplatesByTemplateGroupId request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Templates
            .AsNoTracking()
            .Filter(i => i.TemplateGroupId == request.TemplateGroupId)
            .ToListAsync(cancellationToken)
            .Map(items => items.Map(Mapper.ProjectToViewModel).ToList());
    }
}
