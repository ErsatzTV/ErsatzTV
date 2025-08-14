using ErsatzTV.Application.Tree;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetDecoTemplateTreeHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetDecoTemplateTree, TreeViewModel>
{
    public async Task<TreeViewModel> Handle(
        GetDecoTemplateTree request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<DecoTemplateGroup> decoTemplateGroups = await dbContext.DecoTemplateGroups
            .AsNoTracking()
            .Include(g => g.DecoTemplates)
            .ToListAsync(cancellationToken);

        return Mapper.ProjectToViewModel(decoTemplateGroups);
    }
}
