using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class UpdateRerunCollectionHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IMediaCollectionRepository mediaCollectionRepository,
    ChannelWriter<IBackgroundServiceRequest> channel)
    : IRequestHandler<UpdateRerunCollection, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        UpdateRerunCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, RerunCollection> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(c => ApplyUpdateRequest(dbContext, c, request, cancellationToken));
    }

    private async Task<Unit> ApplyUpdateRequest(
        TvContext dbContext,
        RerunCollection c,
        UpdateRerunCollection request,
        CancellationToken cancellationToken)
    {
        c.Name = request.Name;
        c.CollectionType = request.CollectionType;
        c.CollectionId = request.Collection?.Id;
        c.MultiCollectionId = request.MultiCollection?.Id;
        c.SmartCollectionId = request.SmartCollection?.Id;
        c.MediaItemId = request.MediaItem?.MediaItemId;
        c.FirstRunPlaybackOrder = request.FirstRunPlaybackOrder;
        c.RerunPlaybackOrder = request.RerunPlaybackOrder;

        // rebuild playouts
        if (await dbContext.SaveChangesAsync(cancellationToken) > 0)
        {
            // refresh all playouts that use this rerun collection
            foreach (int playoutId in await mediaCollectionRepository.PlayoutIdsUsingRerunCollection(
                         request.RerunCollectionId))
            {
                await channel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Refresh), cancellationToken);
            }
        }

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, RerunCollection>> Validate(
        TvContext dbContext,
        UpdateRerunCollection request,
        CancellationToken cancellationToken) =>
        (await RerunCollectionMustExist(dbContext, request, cancellationToken), await ValidateName(dbContext, request))
        .Apply((collectionToUpdate, _) => collectionToUpdate);

    private static Task<Validation<BaseError, RerunCollection>> RerunCollectionMustExist(
        TvContext dbContext,
        UpdateRerunCollection updateCollection,
        CancellationToken cancellationToken) =>
        dbContext.RerunCollections
            .SelectOneAsync(c => c.Id, c => c.Id == updateCollection.RerunCollectionId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Rerun collection does not exist."));

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        UpdateRerunCollection updateCollection)
    {
        Validation<BaseError, string> result1 = updateCollection.NotEmpty(c => c.Name)
            .Bind(_ => updateCollection.NotLongerThan(50)(c => c.Name));

        bool duplicateName = await dbContext.RerunCollections
            .AnyAsync(c => c.Id != updateCollection.RerunCollectionId && c.Name == updateCollection.Name);

        Validation<BaseError, Unit> result2 = duplicateName
            ? Fail<BaseError, Unit>("Rerun collection name must be unique")
            : Success<BaseError, Unit>(Unit.Default);

        return (result1, result2).Apply((_, _) => updateCollection.Name);
    }
}
