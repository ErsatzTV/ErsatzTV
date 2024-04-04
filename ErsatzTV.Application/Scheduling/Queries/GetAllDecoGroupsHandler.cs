using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetAllDecoGroupsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllDecoGroups, List<DecoGroupViewModel>>
{
    public async Task<List<DecoGroupViewModel>> Handle(GetAllDecoGroups request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<DecoGroup> decoGroups = await dbContext.DecoGroups
            .AsNoTracking()
            .Include(g => g.Decos)
            .ToListAsync(cancellationToken);

        return decoGroups.Map(Mapper.ProjectToViewModel).ToList();
    }
}
