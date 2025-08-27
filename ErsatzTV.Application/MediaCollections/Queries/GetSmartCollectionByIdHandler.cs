using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetSmartCollectionByIdHandler(IDbContextFactory<TvContext> dbContextFactory) :
    IRequestHandler<GetSmartCollectionById, Option<SmartCollectionViewModel>>
{
    public async Task<Option<SmartCollectionViewModel>> Handle(
        GetSmartCollectionById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.SmartCollections
            .AsNoTracking()
            .SelectOneAsync(c => c.Id, c => c.Id == request.Id, cancellationToken)
            .MapT(ProjectToViewModel);
    }
}
