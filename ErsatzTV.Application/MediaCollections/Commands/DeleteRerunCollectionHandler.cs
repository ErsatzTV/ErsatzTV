using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class DeleteRerunCollectionHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<DeleteRerunCollection, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        DeleteRerunCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, RerunCollection> validation = await RerunCollectionMustExist(
            dbContext,
            request,
            cancellationToken);
        return await validation.Apply(c => DoDeletion(dbContext, c, cancellationToken));
    }

    private static async Task<Unit> DoDeletion(
        TvContext dbContext,
        RerunCollection rerunCollection,
        CancellationToken cancellationToken)
    {
        dbContext.RerunCollections.Remove(rerunCollection);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Default;
    }

    private static Task<Validation<BaseError, RerunCollection>> RerunCollectionMustExist(
        TvContext dbContext,
        DeleteRerunCollection request,
        CancellationToken cancellationToken) =>
        dbContext.RerunCollections
            .SelectOneAsync(c => c.Id, c => c.Id == request.RerunCollectionId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>($"Rerun collection {request.RerunCollectionId} does not exist."));
}
