using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetAllMultiCollectionsHandler : IRequestHandler<GetAllMultiCollections, List<MultiCollectionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetAllMultiCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<MultiCollectionViewModel>> Handle(
        GetAllMultiCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.MultiCollections
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}