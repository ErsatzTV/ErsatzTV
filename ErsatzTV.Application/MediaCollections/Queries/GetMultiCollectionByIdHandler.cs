using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetMultiCollectionByIdHandler : IRequestHandler<GetMultiCollectionById, Option<MultiCollectionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetMultiCollectionByIdHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Option<MultiCollectionViewModel>> Handle(
        GetMultiCollectionById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.MultiCollections
            .Include(mc => mc.MultiCollectionItems)
            .ThenInclude(mc => mc.Collection)
            .Include(mc => mc.MultiCollectionSmartItems)
            .ThenInclude(mc => mc.SmartCollection)
            .SelectOneAsync(c => c.Id, c => c.Id == request.Id)
            .MapT(ProjectToViewModel);
    }
}
