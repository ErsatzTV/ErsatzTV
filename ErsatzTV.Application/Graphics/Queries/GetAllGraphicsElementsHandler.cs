using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Graphics.Mapper;

namespace ErsatzTV.Application.Graphics;

public class GetAllGraphicsElementsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllGraphicsElements, List<GraphicsElementViewModel>>
{
    public async Task<List<GraphicsElementViewModel>> Handle(
        GetAllGraphicsElements request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.GraphicsElements
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).OrderBy(e => e.Name == e.FileName).ThenBy(e => e.Name).ToList());
    }
}
