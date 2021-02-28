using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        RemoveItemsFromCollectionHandler : MediatR.IRequestHandler<RemoveItemsFromCollection, Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public RemoveItemsFromCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<Either<BaseError, Unit>> Handle(
            RemoveItemsFromCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(collection => ApplyAddTelevisionEpisodeRequest(request, collection))
                .Bind(v => v.ToEitherAsync());

        private Task<Unit> ApplyAddTelevisionEpisodeRequest(
            RemoveItemsFromCollection request,
            Collection collection)
        {
            var itemsToRemove = collection.MediaItems
                .Filter(m => request.MediaItemIds.Contains(m.Id))
                .ToList();

            itemsToRemove.ForEach(m => collection.MediaItems.Remove(m));

            return itemsToRemove.Any()
                ? _mediaCollectionRepository.Update(collection).ToUnit()
                : Task.FromResult(Unit.Default);
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
