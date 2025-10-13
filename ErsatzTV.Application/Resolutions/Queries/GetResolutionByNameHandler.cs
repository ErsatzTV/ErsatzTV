using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Resolutions;

public class GetResolutionByNameHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetResolutionByName, Option<ResolutionViewModel>>
{
    public async Task<Option<ResolutionViewModel>> Handle(GetResolutionByName request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Resolutions
            .AsNoTracking()
            .SelectOneAsync(r => r.Name, r => r.Name == request.Name, cancellationToken)
            .MapT(Mapper.ProjectToViewModel);
    }
}
