using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Application.Search;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class RemoveItemsFromCollectionHandler : IRequestHandler<RemoveItemsFromCollection, Either<BaseError, Unit>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;
    private readonly ChannelWriter<ISearchIndexBackgroundServiceRequest> _searchChannel;

    public RemoveItemsFromCollectionHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMediaCollectionRepository mediaCollectionRepository,
        ChannelWriter<IBackgroundServiceRequest> channel,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> searchChannel)
    {
        _dbContextFactory = dbContextFactory;
        _mediaCollectionRepository = mediaCollectionRepository;
        _channel = channel;
        _searchChannel = searchChannel;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        RemoveItemsFromCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Collection> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(c => ApplyRemoveItemsRequest(dbContext, request, c, cancellationToken));
    }

    private async Task<Unit> ApplyRemoveItemsRequest(
        TvContext dbContext,
        RemoveItemsFromCollection request,
        Collection collection,
        CancellationToken cancellationToken)
    {
        var itemsToRemove = collection.MediaItems
            .Filter(m => request.MediaItemIds.Contains(m.Id))
            .ToList();

        itemsToRemove.ForEach(m => collection.MediaItems.Remove(m));

        if (itemsToRemove.Count != 0 && await dbContext.SaveChangesAsync(cancellationToken) > 0)
        {
            await _searchChannel.WriteAsync(
                new ReindexMediaItems(itemsToRemove.Select(mi => mi.Id).ToArray()),
                cancellationToken);

            // refresh all playouts that use this collection
            foreach (int playoutId in await _mediaCollectionRepository.PlayoutIdsUsingCollection(collection.Id))
            {
                await _channel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Refresh), cancellationToken);
            }
        }

        return Unit.Default;
    }

    private static Task<Validation<BaseError, Collection>> Validate(
        TvContext dbContext,
        RemoveItemsFromCollection request,
        CancellationToken cancellationToken) =>
        CollectionMustExist(dbContext, request, cancellationToken);

    private static Task<Validation<BaseError, Collection>> CollectionMustExist(
        TvContext dbContext,
        RemoveItemsFromCollection request,
        CancellationToken cancellationToken) =>
        dbContext.Collections
            .Include(c => c.MediaItems)
            .SelectOneAsync(c => c.Id, c => c.Id == request.MediaCollectionId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Collection does not exist."));
}
