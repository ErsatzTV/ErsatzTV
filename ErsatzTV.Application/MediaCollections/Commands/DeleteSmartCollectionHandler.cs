using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class DeleteSmartCollectionHandler : IRequestHandler<DeleteSmartCollection, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ISearchTargets _searchTargets;

    public DeleteSmartCollectionHandler(IDbContextFactory<TvContext> dbContextFactory, ISearchTargets searchTargets)
    {
        _dbContextFactory = dbContextFactory;
        _searchTargets = searchTargets;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteSmartCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, SmartCollection> validation = await SmartCollectionMustExist(dbContext, request);
        return await validation.Apply(c => DoDeletion(dbContext, c));
    }

    private async Task<Unit> DoDeletion(TvContext dbContext, SmartCollection smartCollection)
    {
        dbContext.SmartCollections.Remove(smartCollection);
        await dbContext.SaveChangesAsync();
        _searchTargets.SearchTargetsChanged();
        return Unit.Default;
    }

    private static Task<Validation<BaseError, SmartCollection>> SmartCollectionMustExist(
        TvContext dbContext,
        DeleteSmartCollection request) =>
        dbContext.SmartCollections
            .SelectOneAsync(c => c.Id, c => c.Id == request.SmartCollectionId)
            .Map(o => o.ToValidation<BaseError>($"SmartCollection {request.SmartCollectionId} does not exist."));
}
