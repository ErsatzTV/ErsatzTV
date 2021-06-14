using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class RemoveItemsFromCollectionHandler :
        MediatR.IRequestHandler<RemoveItemsFromCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public RemoveItemsFromCollectionHandler(
            IDbContextFactory<TvContext> dbContextFactory,
            IMediaCollectionRepository mediaCollectionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _dbContextFactory = dbContextFactory;
            _mediaCollectionRepository = mediaCollectionRepository;
            _channel = channel;
        }

        public async Task<Either<BaseError, Unit>> Handle(
            RemoveItemsFromCollection request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Validation<BaseError, Collection> validation = await Validate(dbContext, request);
            return await validation.Apply(c => ApplyRemoveItemsRequest(dbContext, request, c));
        }

        private async Task<Unit> ApplyRemoveItemsRequest(
            TvContext dbContext,
            RemoveItemsFromCollection request,
            Collection collection)
        {
            var itemsToRemove = collection.MediaItems
                .Filter(m => request.MediaItemIds.Contains(m.Id))
                .ToList();

            itemsToRemove.ForEach(m => collection.MediaItems.Remove(m));

            if (itemsToRemove.Any() && await dbContext.SaveChangesAsync() > 0)
            {
                // rebuild all playouts that use this collection
                foreach (int playoutId in await _mediaCollectionRepository.PlayoutIdsUsingCollection(collection.Id))
                {
                    await _channel.WriteAsync(new BuildPlayout(playoutId, true));
                }
            }

            return Unit.Default;
        }

        private static Task<Validation<BaseError, Collection>> Validate(
            TvContext dbContext,
            RemoveItemsFromCollection request) =>
            CollectionMustExist(dbContext, request);

        private static Task<Validation<BaseError, Collection>> CollectionMustExist(
            TvContext dbContext,
            RemoveItemsFromCollection request) =>
            dbContext.Collections
                .Include(c => c.MediaItems)
                .SelectOneAsync(c => c.Id, c => c.Id == request.MediaCollectionId)
                .Map(o => o.ToValidation<BaseError>("Collection does not exist."));
    }
}
