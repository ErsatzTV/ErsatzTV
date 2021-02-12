using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddItemsToSimpleMediaCollectionHandler : MediatR.IRequestHandler<AddItemsToSimpleMediaCollection,
            Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly IMediaItemRepository _mediaItemRepository;

        public AddItemsToSimpleMediaCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            IMediaItemRepository mediaItemRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _mediaItemRepository = mediaItemRepository;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddItemsToSimpleMediaCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(ApplyAddItemsRequest)
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddItemsRequest(RequestParameters parameters)
        {
            foreach (MediaItem item in parameters.ItemsToAdd.Where(
                item => parameters.Collection.Items.All(i => i.Id != item.Id)))
            {
                parameters.Collection.Items.Add(item);
            }

            await _mediaCollectionRepository.Update(parameters.Collection);

            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>>
            Validate(AddItemsToSimpleMediaCollection request) =>
            (await SimpleMediaCollectionMustExist(request), await ValidateItems(request))
            .Apply(
                (simpleMediaCollectionToUpdate, itemsToAdd) =>
                    new RequestParameters(simpleMediaCollectionToUpdate, itemsToAdd));

        private Task<Validation<BaseError, SimpleMediaCollection>> SimpleMediaCollectionMustExist(
            AddItemsToSimpleMediaCollection updateSimpleMediaCollection) =>
            _mediaCollectionRepository.GetSimpleMediaCollection(updateSimpleMediaCollection.MediaCollectionId)
                .Map(v => v.ToValidation<BaseError>("SimpleMediaCollection does not exist."));

        private Task<Validation<BaseError, List<MediaItem>>> ValidateItems(
            AddItemsToSimpleMediaCollection request) =>
            LoadAllMediaItems(request)
                .Map(v => v.ToValidation<BaseError>("MediaItem does not exist"));

        private async Task<Option<List<MediaItem>>> LoadAllMediaItems(AddItemsToSimpleMediaCollection request)
        {
            var items = (await request.ItemIds.Map(async id => await _mediaItemRepository.Get(id)).Sequence())
                .ToList();
            if (items.Any(i => i.IsNone))
            {
                return None;
            }

            return items.Somes().ToList();
        }

        private record RequestParameters(SimpleMediaCollection Collection, List<MediaItem> ItemsToAdd);
    }
}
