using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class ReplaceSimpleMediaCollectionItemsHandler : IRequestHandler<ReplaceSimpleMediaCollectionItems,
        Either<BaseError, List<MediaItemViewModel>>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly IMediaItemRepository _mediaItemRepository;

        public ReplaceSimpleMediaCollectionItemsHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            IMediaItemRepository mediaItemRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _mediaItemRepository = mediaItemRepository;
        }

        public Task<Either<BaseError, List<MediaItemViewModel>>> Handle(
            ReplaceSimpleMediaCollectionItems request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(mediaItems => PersistItems(request, mediaItems))
                .Bind(v => v.ToEitherAsync());

        private async Task<List<MediaItemViewModel>> PersistItems(
            ReplaceSimpleMediaCollectionItems request,
            List<MediaItem> mediaItems)
        {
            await _mediaCollectionRepository.ReplaceItems(request.MediaCollectionId, mediaItems);
            return mediaItems.Map(MediaItems.Mapper.ProjectToViewModel).ToList();
        }

        private Task<Validation<BaseError, List<MediaItem>>> Validate(ReplaceSimpleMediaCollectionItems request) =>
            MediaCollectionMustExist(request).BindT(_ => MediaItemsMustExist(request));

        private async Task<Validation<BaseError, SimpleMediaCollection>> MediaCollectionMustExist(
            ReplaceSimpleMediaCollectionItems request) =>
            (await _mediaCollectionRepository.GetSimpleMediaCollection(request.MediaCollectionId))
            .ToValidation<BaseError>("[MediaCollectionId] does not exist.");

        private async Task<Validation<BaseError, List<MediaItem>>> MediaItemsMustExist(
            ReplaceSimpleMediaCollectionItems replaceItems)
        {
            var allMediaItems = (await replaceItems.MediaItemIds.Map(i => _mediaItemRepository.Get(i)).Sequence())
                .ToList();
            if (allMediaItems.Any(x => x.IsNone))
            {
                return BaseError.New("[MediaItemId] does not exist");
            }

            return allMediaItems.Sequence().ValueUnsafe().ToList();
        }
    }
}
