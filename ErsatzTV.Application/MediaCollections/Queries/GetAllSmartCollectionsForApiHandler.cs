using ErsatzTV.Core.Api.SmartCollections;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetAllSmartCollectionsForApiHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllSmartCollectionsForApi, List<SmartCollectionResponseModel>>
{
    public async Task<List<SmartCollectionResponseModel>> Handle(
        GetAllSmartCollectionsForApi request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<SmartCollection> ffmpegProfiles = await dbContext.SmartCollections
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return ffmpegProfiles.Map(ProjectToResponseModel).ToList();
    }
}
