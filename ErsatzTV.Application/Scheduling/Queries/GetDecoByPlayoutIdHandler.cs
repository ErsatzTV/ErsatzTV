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
            .AsNoTracking()
            .Include(p => p.Deco)
            .ThenInclude(d => d.DecoGroup)
            .Include(p => p.Deco)
            .ThenInclude(d => d.DecoWatermarks)
            .ThenInclude(d => d.Watermark)
            .Include(p => p.Deco)
            .ThenInclude(d => d.DecoGraphicsElements)
            .ThenInclude(d => d.GraphicsElement)
            .Include(p => p.Deco)
            .ThenInclude(d => d.BreakContent)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId && p.DecoId != null, cancellationToken)
            .MapT(p => Mapper.ProjectToViewModel(p.Deco));
    }
}
