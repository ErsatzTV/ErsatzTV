﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class DeleteCollectionHandler : IRequestHandler<DeleteCollection, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ISearchTargets _searchTargets;

    public DeleteCollectionHandler(IDbContextFactory<TvContext> dbContextFactory, ISearchTargets searchTargets)
    {
        _dbContextFactory = dbContextFactory;
        _searchTargets = searchTargets;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Collection> validation = await CollectionMustExist(dbContext, request);
        return await validation.Apply(c => DoDeletion(dbContext, c));
    }

    private async Task<Unit> DoDeletion(TvContext dbContext, Collection collection)
    {
        dbContext.Collections.Remove(collection);
        await dbContext.SaveChangesAsync();
        _searchTargets.SearchTargetsChanged();
        return Unit.Default;
    }

    private static Task<Validation<BaseError, Collection>> CollectionMustExist(
        TvContext dbContext,
        DeleteCollection request) =>
        dbContext.Collections
            .SelectOneAsync(c => c.Id, c => c.Id == request.CollectionId)
            .Map(o => o.ToValidation<BaseError>($"Collection {request.CollectionId} does not exist."));
}
