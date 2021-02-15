using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaItems.Commands
{
    public class
        RefreshMediaItemMetadataHandler : MediatR.IRequestHandler<RefreshMediaItemMetadata, Either<BaseError, Unit>>
    {
        private readonly ILocalMetadataProvider _localMetadataProvider;
        private readonly IMediaItemRepository _mediaItemRepository;

        public RefreshMediaItemMetadataHandler(
            IMediaItemRepository mediaItemRepository,
            ILocalMetadataProvider localMetadataProvider)
        {
            _mediaItemRepository = mediaItemRepository;
            _localMetadataProvider = localMetadataProvider;
        }

        public Task<Either<BaseError, Unit>> Handle(
            RefreshMediaItemMetadata request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(RefreshMetadata)
                .Bind(v => v.ToEitherAsync());

        private Task<Validation<BaseError, MediaItem>> Validate(RefreshMediaItemMetadata request) =>
            MediaItemMustExist(request).BindT(PathMustExist);

        private Task<Validation<BaseError, MediaItem>> MediaItemMustExist(
            RefreshMediaItemMetadata refreshMediaItemMetadata) =>
            _mediaItemRepository.Get(refreshMediaItemMetadata.MediaItemId)
                .Map(
                    maybeItem => maybeItem.ToValidation<BaseError>(
                        $"[MediaItem] {refreshMediaItemMetadata.MediaItemId} does not exist."));

        private Validation<BaseError, MediaItem> PathMustExist(MediaItem mediaItem) =>
            Some(mediaItem)
                .Filter(item => File.Exists(item.Path))
                .ToValidation<BaseError>($"[Path] '{mediaItem.Path}' does not exist on the file system");

        private Task<Unit> RefreshMetadata(MediaItem mediaItem) => Task.CompletedTask.ToUnit();
        // TODO: reimplement this
        // _localMetadataProvider.RefreshMetadata(mediaItem).ToUnit();
    }
}
