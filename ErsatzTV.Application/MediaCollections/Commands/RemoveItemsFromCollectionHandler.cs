using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        RemoveItemsFromCollectionHandler : MediatR.IRequestHandler<RemoveItemsFromCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public RemoveItemsFromCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, Unit>> Handle(
            RemoveItemsFromCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(collection => ApplyRemoveItemsRequest(request, collection))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyRemoveItemsRequest(
            RemoveItemsFromCollection request,
            Collection collection)
        {
            var itemsToRemove = collection.MediaItems
                .Filter(m => request.MediaItemIds.Contains(m.Id))
                .ToList();

            itemsToRemove.ForEach(m => collection.MediaItems.Remove(m));

            if (itemsToRemove.Any() && await _mediaCollectionRepository.Update(collection))
            {
                // rebuild all playouts that use this collection
                foreach (int playoutId in await _mediaCollectionRepository.PlayoutIdsUsingCollection(collection.Id))
                {
                    await _channel.WriteAsync(new BuildPlayout(playoutId, true));
                }
            }

            return Unit.Default;
        }

        private Task<Validation<BaseError, Collection>> Validate(
            RemoveItemsFromCollection request) =>
            CollectionMustExist(request);

        private Task<Validation<BaseError, Collection>> CollectionMustExist(
            RemoveItemsFromCollection updateCollection) =>
            _mediaCollectionRepository.GetCollectionWithItems(updateCollection.MediaCollectionId)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));
    }
}
