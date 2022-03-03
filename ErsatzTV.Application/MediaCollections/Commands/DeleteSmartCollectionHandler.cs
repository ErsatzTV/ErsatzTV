using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class DeleteSmartCollectionHandler : MediatR.IRequestHandler<DeleteSmartCollection, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public DeleteSmartCollectionHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteSmartCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();

        Validation<BaseError, SmartCollection> validation = await SmartCollectionMustExist(dbContext, request);
        return await LanguageExtensions.Apply(validation, c => DoDeletion(dbContext, c));
    }

    private static Task<Unit> DoDeletion(TvContext dbContext, SmartCollection smartCollection)
    {
        dbContext.SmartCollections.Remove(smartCollection);
        return dbContext.SaveChangesAsync().ToUnit();
    }

    private static Task<Validation<BaseError, SmartCollection>> SmartCollectionMustExist(
        TvContext dbContext,
        DeleteSmartCollection request) =>
        dbContext.SmartCollections
            .SelectOneAsync(c => c.Id, c => c.Id == request.SmartCollectionId)
            .Map(o => o.ToValidation<BaseError>($"SmartCollection {request.SmartCollectionId} does not exist."));
}