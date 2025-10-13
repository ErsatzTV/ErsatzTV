using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.FFmpegProfiles;

public class GetResolutionByNameHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetResolutionByName, Option<int>>
{
    public async Task<Option<int>> Handle(GetResolutionByName request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Resolutions
            .AsNoTracking()
            .SelectOneAsync(r => r.Name, r => r.Name == request.Name, cancellationToken)
            .MapT(r => r.Id);
    }
}
