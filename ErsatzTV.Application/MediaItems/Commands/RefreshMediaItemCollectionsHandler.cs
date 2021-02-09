using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaItems.Commands
{
    public class
        RefreshMediaItemCollectionsHandler : MediatR.IRequestHandler<RefreshMediaItemCollections,
            Either<BaseError, Unit>>
    {
        private readonly IMediaItemRepository _mediaItemRepository;
        private readonly ISmartCollectionBuilder _smartCollectionBuilder;

        public RefreshMediaItemCollectionsHandler(
            IMediaItemRepository mediaItemRepository,
            ISmartCollectionBuilder smartCollectionBuilder)
        {
            _mediaItemRepository = mediaItemRepository;
            _smartCollectionBuilder = smartCollectionBuilder;
        }

        public Task<Either<BaseError, Unit>> Handle(
            RefreshMediaItemCollections request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(RefreshCollections)
                .Bind(v => v.ToEitherAsync());

        private Task<Validation<BaseError, MediaItem>> Validate(RefreshMediaItemCollections request) =>
            MediaItemMustExist(request);

        private Task<Validation<BaseError, MediaItem>> MediaItemMustExist(
            RefreshMediaItemCollections refreshMediaItemCollections) =>
            _mediaItemRepository.Get(refreshMediaItemCollections.MediaItemId)
                .Map(
                    maybeItem => maybeItem.ToValidation<BaseError>(
                        $"[MediaItem] {refreshMediaItemCollections.MediaItemId} does not exist."));

        private Task<Unit> RefreshCollections(MediaItem mediaItem) =>
            _smartCollectionBuilder.RefreshSmartCollections(mediaItem).ToUnit();
    }
}
