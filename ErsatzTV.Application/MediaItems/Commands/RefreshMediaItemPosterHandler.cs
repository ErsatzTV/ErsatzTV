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
        RefreshMediaItemPosterHandler : MediatR.IRequestHandler<RefreshMediaItemPoster,
            Either<BaseError, Unit>>
    {
        private readonly ILocalPosterProvider _localPosterProvider;
        private readonly IMediaItemRepository _mediaItemRepository;

        public RefreshMediaItemPosterHandler(
            IMediaItemRepository mediaItemRepository,
            ILocalPosterProvider localPosterProvider)
        {
            _mediaItemRepository = mediaItemRepository;
            _localPosterProvider = localPosterProvider;
        }

        public Task<Either<BaseError, Unit>> Handle(
            RefreshMediaItemPoster request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(RefreshPoster)
                .Bind(v => v.ToEitherAsync());

        private Task<Validation<BaseError, MediaItem>> Validate(RefreshMediaItemPoster request) =>
            MediaItemMustExist(request);

        private Task<Validation<BaseError, MediaItem>> MediaItemMustExist(RefreshMediaItemPoster request) =>
            _mediaItemRepository.Get(request.MediaItemId)
                .Map(
                    maybeItem => maybeItem.ToValidation<BaseError>(
                        $"[MediaItem] {request.MediaItemId} does not exist."));

        private Task<Unit> RefreshPoster(MediaItem mediaItem) =>
            _localPosterProvider.RefreshPoster(mediaItem).ToUnit();
    }
}
