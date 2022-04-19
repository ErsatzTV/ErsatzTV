using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetAllSmartCollectionsHandler : IRequestHandler<GetAllSmartCollections, List<SmartCollectionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetAllSmartCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<SmartCollectionViewModel>> Handle(
        GetAllSmartCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.SmartCollections
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
