﻿using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class UpdateCollectionCustomOrderHandler : IRequestHandler<UpdateCollectionCustomOrder, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;

    public UpdateCollectionCustomOrderHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMediaCollectionRepository mediaCollectionRepository,
        ChannelWriter<IBackgroundServiceRequest> channel)
    {
        _dbContextFactory = dbContextFactory;
        _mediaCollectionRepository = mediaCollectionRepository;
        _channel = channel;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        UpdateCollectionCustomOrder request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Collection> validation = await Validate(dbContext, request);
        return await validation.Apply(c => ApplyUpdateRequest(dbContext, c, request));
    }

    private async Task<Unit> ApplyUpdateRequest(
        TvContext dbContext,
        Collection c,
        UpdateCollectionCustomOrder request)
    {
        foreach (MediaItemCustomOrder updateItem in request.MediaItemCustomOrders)
        {
            Option<CollectionItem> maybeCollectionItem = c.CollectionItems
                .FirstOrDefault(ci => ci.MediaItemId == updateItem.MediaItemId);

            foreach (CollectionItem collectionItem in maybeCollectionItem)
            {
                collectionItem.CustomIndex = updateItem.CustomIndex;
            }
        }

        if (await dbContext.SaveChangesAsync() > 0)
        {
            // refresh all playouts that use this collection
            foreach (int playoutId in await _mediaCollectionRepository
                         .PlayoutIdsUsingCollection(request.CollectionId))
            {
                await _channel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Refresh));
            }
        }

        return Unit.Default;
    }

    private static Task<Validation<BaseError, Collection>> Validate(
        TvContext dbContext,
        UpdateCollectionCustomOrder request) =>
        CollectionMustExist(dbContext, request);

    private static Task<Validation<BaseError, Collection>> CollectionMustExist(
        TvContext dbContext,
        UpdateCollectionCustomOrder request) =>
        dbContext.Collections
            .Include(c => c.CollectionItems)
            .SelectOneAsync(c => c.Id, c => c.Id == request.CollectionId)
            .Map(o => o.ToValidation<BaseError>("Collection does not exist."));
}
