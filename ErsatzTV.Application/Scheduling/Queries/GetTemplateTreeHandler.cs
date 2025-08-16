using ErsatzTV.Application.Tree;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetTemplateTreeHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetTemplateTree, TreeViewModel>
{
    public async Task<TreeViewModel> Handle(
        GetTemplateTree request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<TemplateGroup> templateGroups = await dbContext.TemplateGroups
            .AsNoTracking()
            .Include(g => g.Templates)
            .ToListAsync(cancellationToken);

        return Mapper.ProjectToViewModel(templateGroups);
    }
}
