using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        RemoveItemsFromCollectionHandler : IRequestHandler<RemoveItemsFromCollection,
            Either<BaseError, CollectionUpdateResult>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public RemoveItemsFromCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<Either<BaseError, CollectionUpdateResult>> Handle(
            RemoveItemsFromCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(collection => ApplyRemoveItemsRequest(request, collection))
                .Bind(v => v.ToEitherAsync());

        private async Task<CollectionUpdateResult> ApplyRemoveItemsRequest(
            RemoveItemsFromCollection request,
            Collection collection)
        {
            var itemsToRemove = collection.MediaItems
                .Filter(m => request.MediaItemIds.Contains(m.Id))
                .ToList();

            itemsToRemove.ForEach(m => collection.MediaItems.Remove(m));

            if (itemsToRemove.Any() && await _mediaCollectionRepository.Update(collection))
            {
                return new CollectionUpdateResult
                {
                    ModifiedPlayoutIds = await _mediaCollectionRepository.PlayoutIdsUsingCollection(collection.Id)
                };
            }

            return new CollectionUpdateResult();
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
