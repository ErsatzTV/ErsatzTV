using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetAllDecoTemplateGroupsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllDecoTemplateGroups, List<DecoTemplateGroupViewModel>>
{
    public async Task<List<DecoTemplateGroupViewModel>> Handle(
        GetAllDecoTemplateGroups request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<DecoTemplateGroup> blockGroups = await dbContext.DecoTemplateGroups
            .AsNoTracking()
            .Include(g => g.DecoTemplates)
            .ToListAsync(cancellationToken);

        return blockGroups.Map(Mapper.ProjectToViewModel).ToList();
    }
}
