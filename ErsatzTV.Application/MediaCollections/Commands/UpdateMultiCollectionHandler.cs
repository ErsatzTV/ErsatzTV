using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCollections;

public class UpdateMultiCollectionHandler : MediatR.IRequestHandler<UpdateMultiCollection, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;

    public UpdateMultiCollectionHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMediaCollectionRepository mediaCollectionRepository,
        ChannelWriter<IBackgroundServiceRequest> channel)
    {
        _dbContextFactory = dbContextFactory;
        _mediaCollectionRepository = mediaCollectionRepository;
        _channel = channel;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        UpdateMultiCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Validation<BaseError, MultiCollection> validation = await Validate(dbContext, request);
        return await LanguageExtensions.Apply(validation, c => ApplyUpdateRequest(dbContext, c, request));
    }

    private async Task<Unit> ApplyUpdateRequest(TvContext dbContext, MultiCollection c, UpdateMultiCollection request)
    {
        c.Name = request.Name;
            
        // save name first so playouts don't get rebuilt for a name change
        await dbContext.SaveChangesAsync();

        var toAdd = request.Items
            .Filter(i => i.CollectionId.HasValue)
            // ReSharper disable once PossibleInvalidOperationException
            .Filter(i => c.MultiCollectionItems.All(i2 => i2.CollectionId != i.CollectionId.Value))
            .Map(i => new MultiCollectionItem
            {
                // ReSharper disable once PossibleInvalidOperationException
                CollectionId = i.CollectionId.Value,
                MultiCollectionId = c.Id,
                ScheduleAsGroup = i.ScheduleAsGroup,
                PlaybackOrder = i.PlaybackOrder
            })
            .ToList();
        var toRemove = c.MultiCollectionItems
            .Filter(i => request.Items.All(i2 => i2.CollectionId != i.CollectionId))
            .ToList();
            
        // remove items that are no longer present
        c.MultiCollectionItems.RemoveAll(toRemove.Contains);
            
        // update existing items
        foreach (MultiCollectionItem item in c.MultiCollectionItems)
        {
            foreach (UpdateMultiCollectionItem incoming in request.Items.Filter(
                         i => i.CollectionId == item.CollectionId))
            {
                item.ScheduleAsGroup = incoming.ScheduleAsGroup;
                item.PlaybackOrder = incoming.PlaybackOrder;
            }
        }

        // add new items
        c.MultiCollectionItems.AddRange(toAdd);

        var toAddSmart = request.Items
            .Filter(i => i.SmartCollectionId.HasValue)
            // ReSharper disable once PossibleInvalidOperationException
            .Filter(i => c.MultiCollectionSmartItems.All(i2 => i2.SmartCollectionId != i.SmartCollectionId.Value))
            .Map(i => new MultiCollectionSmartItem
            {
                // ReSharper disable once PossibleInvalidOperationException
                SmartCollectionId = i.SmartCollectionId.Value,
                MultiCollectionId = c.Id,
                ScheduleAsGroup = i.ScheduleAsGroup,
                PlaybackOrder = i.PlaybackOrder
            })
            .ToList();
        var toRemoveSmart = c.MultiCollectionSmartItems
            .Filter(i => request.Items.All(i2 => i2.SmartCollectionId != i.SmartCollectionId))
            .ToList();
            
        // remove items that are no longer present
        c.MultiCollectionSmartItems.RemoveAll(toRemoveSmart.Contains);
            
        // update existing items
        foreach (MultiCollectionSmartItem item in c.MultiCollectionSmartItems)
        {
            foreach (UpdateMultiCollectionItem incoming in request.Items.Filter(
                         i => i.SmartCollectionId == item.SmartCollectionId))
            {
                item.ScheduleAsGroup = incoming.ScheduleAsGroup;
                item.PlaybackOrder = incoming.PlaybackOrder;
            }
        }

        // add new items
        c.MultiCollectionSmartItems.AddRange(toAddSmart);

        // rebuild playouts
        if (await dbContext.SaveChangesAsync() > 0)
        {
            // rebuild all playouts that use this collection
            foreach (int playoutId in await _mediaCollectionRepository.PlayoutIdsUsingMultiCollection(
                         request.MultiCollectionId))
            {
                await _channel.WriteAsync(new BuildPlayout(playoutId, true));
            }
        }

        return Unit.Default;
    }

    private static async Task<Validation<BaseError, MultiCollection>> Validate(
        TvContext dbContext,
        UpdateMultiCollection request) =>
        (await MultiCollectionMustExist(dbContext, request), await ValidateName(dbContext, request))
        .Apply((collectionToUpdate, _) => collectionToUpdate);

    private static Task<Validation<BaseError, MultiCollection>> MultiCollectionMustExist(
        TvContext dbContext,
        UpdateMultiCollection updateCollection) =>
        dbContext.MultiCollections
            .Include(mc => mc.MultiCollectionItems)
            .Include(mc => mc.MultiCollectionSmartItems)
            .SelectOneAsync(c => c.Id, c => c.Id == updateCollection.MultiCollectionId)
            .Map(o => o.ToValidation<BaseError>("MultiCollection does not exist."));

    private static async Task<Validation<BaseError, string>> ValidateName(TvContext dbContext, UpdateMultiCollection updateMultiCollection)
    {
        List<string> allNames = await dbContext.MultiCollections
            .Filter(mc => mc.Id != updateMultiCollection.MultiCollectionId)
            .Map(c => c.Name)
            .ToListAsync();

        Validation<BaseError, string> result1 = updateMultiCollection.NotEmpty(c => c.Name)
            .Bind(_ => updateMultiCollection.NotLongerThan(50)(c => c.Name));

        var result2 = Optional(updateMultiCollection.Name)
            .Where(name => !allNames.Contains(name))
            .ToValidation<BaseError>("MultiCollection name must be unique");

        return (result1, result2).Apply((_, _) => updateMultiCollection.Name);
    }
}