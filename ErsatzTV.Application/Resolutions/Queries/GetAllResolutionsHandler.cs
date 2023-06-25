using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Resolutions.Mapper;

namespace ErsatzTV.Application.Resolutions;

public class GetAllResolutionsHandler : IRequestHandler<GetAllResolutions, List<ResolutionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetAllResolutionsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<ResolutionViewModel>> Handle(
        GetAllResolutions request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Resolutions
            .ToListAsync(cancellationToken)
            .Map(list => list.OrderBy(r => r.Width).ThenBy(r => r.Height).Map(ProjectToViewModel).ToList());
    }
}
