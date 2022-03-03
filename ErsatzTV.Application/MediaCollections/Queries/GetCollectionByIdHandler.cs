using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetCollectionByIdHandler :
    IRequestHandler<GetCollectionById, Option<MediaCollectionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetCollectionByIdHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Option<MediaCollectionViewModel>> Handle(
        GetCollectionById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Collections
            .SelectOneAsync(c => c.Id, c => c.Id == request.Id)
            .MapT(ProjectToViewModel);
    }
}