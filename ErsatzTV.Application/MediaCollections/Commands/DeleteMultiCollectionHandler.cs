using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class DeleteMultiCollectionHandler : MediatR.IRequestHandler<DeleteMultiCollection, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public DeleteMultiCollectionHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteMultiCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();

        Validation<BaseError, MultiCollection> validation = await MultiCollectionMustExist(dbContext, request);
        return await validation.Apply(c => DoDeletion(dbContext, c));
    }

    private static Task<Unit> DoDeletion(TvContext dbContext, MultiCollection multiCollection)
    {
        dbContext.MultiCollections.Remove(multiCollection);
        return dbContext.SaveChangesAsync().ToUnit();
    }

    private static Task<Validation<BaseError, MultiCollection>> MultiCollectionMustExist(
        TvContext dbContext,
        DeleteMultiCollection request) =>
        dbContext.MultiCollections
            .SelectOneAsync(c => c.Id, c => c.Id == request.MultiCollectionId)
            .Map(o => o.ToValidation<BaseError>($"MultiCollection {request.MultiCollectionId} does not exist."));
}