using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetDecoByPlayoutIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetDecoByPlayoutId, Option<DecoViewModel>>
{
    public async Task<Option<DecoViewModel>> Handle(GetDecoByPlayoutId request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Playouts
            .Include(p => p.Deco)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId && p.DecoId != null)
            .MapT(p => Mapper.ProjectToViewModel(p.Deco));
    }
}
