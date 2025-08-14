using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetAllTemplateGroupsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllTemplateGroups, List<TemplateGroupViewModel>>
{
    public async Task<List<TemplateGroupViewModel>> Handle(
        GetAllTemplateGroups request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<TemplateGroup> templateGroups = await dbContext.TemplateGroups
            .AsNoTracking()
            .Include(g => g.Templates)
            .ToListAsync(cancellationToken);

        return templateGroups.OrderBy(tg => tg.Name).Map(Mapper.ProjectToViewModel).ToList();
    }
}
