﻿using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class UpdateCollectionHandler : IRequestHandler<UpdateCollection, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;
    private readonly ISearchTargets _searchTargets;

    public UpdateCollectionHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMediaCollectionRepository mediaCollectionRepository,
        ChannelWriter<IBackgroundServiceRequest> channel,
        ISearchTargets searchTargets)
    {
        _dbContextFactory = dbContextFactory;
        _mediaCollectionRepository = mediaCollectionRepository;
        _channel = channel;
        _searchTargets = searchTargets;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        UpdateCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Collection> validation = await Validate(dbContext, request);
        return await validation.Apply(c => ApplyUpdateRequest(dbContext, c, request));
    }

    private async Task<Unit> ApplyUpdateRequest(TvContext dbContext, Collection c, UpdateCollection request)
    {
        c.Name = request.Name;
        foreach (bool useCustomPlaybackOrder in request.UseCustomPlaybackOrder)
        {
            c.UseCustomPlaybackOrder = useCustomPlaybackOrder;
        }

        if (await dbContext.SaveChangesAsync() > 0 && request.UseCustomPlaybackOrder.IsSome)
        {
            // refresh all playouts that use this collection
            foreach (int playoutId in await _mediaCollectionRepository.PlayoutIdsUsingCollection(
                         request.CollectionId))
            {
                await _channel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Refresh));
            }
        }

        _searchTargets.SearchTargetsChanged();

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, Collection>> Validate(
        TvContext dbContext,
        UpdateCollection request) =>
        (await CollectionMustExist(dbContext, request), ValidateName(request))
        .Apply((collectionToUpdate, _) => collectionToUpdate);

    private static Task<Validation<BaseError, Collection>> CollectionMustExist(
        TvContext dbContext,
        UpdateCollection updateCollection) =>
        dbContext.Collections
            .SelectOneAsync(c => c.Id, c => c.Id == updateCollection.CollectionId)
            .Map(o => o.ToValidation<BaseError>("Collection does not exist."));

    private static Validation<BaseError, string> ValidateName(UpdateCollection updateSimpleMediaCollection) =>
        updateSimpleMediaCollection.NotEmpty(c => c.Name)
            .Bind(_ => updateSimpleMediaCollection.NotLongerThan(50)(c => c.Name));
}
