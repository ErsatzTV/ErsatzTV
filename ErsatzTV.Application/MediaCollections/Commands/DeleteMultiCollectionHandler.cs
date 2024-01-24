using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class DeleteMultiCollectionHandler : IRequestHandler<DeleteMultiCollection, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ISearchTargets _searchTargets;

    public DeleteMultiCollectionHandler(IDbContextFactory<TvContext> dbContextFactory, ISearchTargets searchTargets)
    {
        _dbContextFactory = dbContextFactory;
        _searchTargets = searchTargets;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteMultiCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        Validation<BaseError, MultiCollection> validation = await MultiCollectionMustExist(dbContext, request);
        return await validation.Apply(c => DoDeletion(dbContext, c));
    }

    private async Task<Unit> DoDeletion(TvContext dbContext, MultiCollection multiCollection)
    {
        dbContext.MultiCollections.Remove(multiCollection);
        await dbContext.SaveChangesAsync();
        _searchTargets.SearchTargetsChanged();
        return Unit.Default;
    }

    private static Task<Validation<BaseError, MultiCollection>> MultiCollectionMustExist(
        TvContext dbContext,
        DeleteMultiCollection request) =>
        dbContext.MultiCollections
            .SelectOneAsync(c => c.Id, c => c.Id == request.MultiCollectionId)
            .Map(o => o.ToValidation<BaseError>($"MultiCollection {request.MultiCollectionId} does not exist."));
}
