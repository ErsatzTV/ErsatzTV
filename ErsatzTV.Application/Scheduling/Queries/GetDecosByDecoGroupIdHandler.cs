using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetDecosByDecoGroupIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetDecosByDecoGroupId, List<DecoViewModel>>
{
    public async Task<List<DecoViewModel>> Handle(GetDecosByDecoGroupId request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Deco> decos = await dbContext.Decos
            .Filter(b => b.DecoGroupId == request.DecoGroupId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return decos.Map(Mapper.ProjectToViewModel).ToList();
    }
}
