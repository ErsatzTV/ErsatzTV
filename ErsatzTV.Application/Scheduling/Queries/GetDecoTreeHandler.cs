using ErsatzTV.Application.Tree;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetDecoTreeHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetDecoTree, TreeViewModel>
{
    public async Task<TreeViewModel> Handle(
        GetDecoTree request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<DecoGroup> templateGroups = await dbContext.DecoGroups
            .AsNoTracking()
            .Include(g => g.Decos)
            .ToListAsync(cancellationToken);

        return Mapper.ProjectToViewModel(templateGroups);
    }
}
