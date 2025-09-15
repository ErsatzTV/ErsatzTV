﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class CreateRerunCollectionHandler(IDbContextFactory<TvContext> dbContextFactory) :
    IRequestHandler<CreateRerunCollection, Either<BaseError, RerunCollectionViewModel>>
{
    public async Task<Either<BaseError, RerunCollectionViewModel>> Handle(
        CreateRerunCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, RerunCollection> validation = await Validate(dbContext, request);
        return await validation.Apply(c => PersistCollection(dbContext, c));
    }

    private static async Task<RerunCollectionViewModel> PersistCollection(
        TvContext dbContext,
        RerunCollection collection)
    {
        await dbContext.RerunCollections.AddAsync(collection);
        await dbContext.SaveChangesAsync();
        return ProjectToViewModel(collection);
    }

    private static Task<Validation<BaseError, RerunCollection>> Validate(
        TvContext dbContext,
        CreateRerunCollection request) =>
        ValidateName(dbContext, request).MapT(name => new RerunCollection
        {
            Name = name,
            CollectionType = request.CollectionType,
            CollectionId = request.Collection?.Id,
            MultiCollectionId = request.MultiCollection?.Id,
            SmartCollectionId = request.SmartCollection?.Id,
            MediaItemId = request.MediaItem?.MediaItemId,
            FirstRunPlaybackOrder = request.FirstRunPlaybackOrder,
            RerunPlaybackOrder = request.RerunPlaybackOrder
        });

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        CreateRerunCollection createCollection)
    {
        List<string> allNames = await dbContext.RerunCollections
            .Map(c => c.Name)
            .ToListAsync();

        Validation<BaseError, string> result1 = createCollection.NotEmpty(c => c.Name)
            .Bind(_ => createCollection.NotLongerThan(50)(c => c.Name));

        var result2 = Optional(createCollection.Name)
            .Where(name => !allNames.Contains(name))
            .ToValidation<BaseError>("Rerun collection name must be unique");

        return (result1, result2).Apply((_, _) => createCollection.Name);
    }
}
