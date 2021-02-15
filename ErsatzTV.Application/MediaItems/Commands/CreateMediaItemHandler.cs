using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static LanguageExt.Prelude;
using static ErsatzTV.Application.MediaItems.Mapper;

namespace ErsatzTV.Application.MediaItems.Commands
{
    public class CreateMediaItemHandler : IRequestHandler<CreateMediaItem, Either<BaseError, MediaItemViewModel>>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly ILocalMetadataProvider _localMetadataProvider;
        private readonly ILocalPosterProvider _localPosterProvider;
        private readonly ILocalStatisticsProvider _localStatisticsProvider;
        private readonly IMediaItemRepository _mediaItemRepository;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly ISmartCollectionBuilder _smartCollectionBuilder;

        public CreateMediaItemHandler(
            IMediaItemRepository mediaItemRepository,
            IMediaSourceRepository mediaSourceRepository,
            IConfigElementRepository configElementRepository,
            ISmartCollectionBuilder smartCollectionBuilder,
            ILocalMetadataProvider localMetadataProvider,
            ILocalStatisticsProvider localStatisticsProvider,
            ILocalPosterProvider localPosterProvider)
        {
            _mediaItemRepository = mediaItemRepository;
            _mediaSourceRepository = mediaSourceRepository;
            _configElementRepository = configElementRepository;
            _smartCollectionBuilder = smartCollectionBuilder;
            _localMetadataProvider = localMetadataProvider;
            _localStatisticsProvider = localStatisticsProvider;
            _localPosterProvider = localPosterProvider;
        }

        public Task<Either<BaseError, MediaItemViewModel>> Handle(
            CreateMediaItem request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(PersistMediaItem)
                .Bind(v => v.ToEitherAsync());

        private async Task<MediaItemViewModel> PersistMediaItem(RequestParameters parameters)
        {
            await _mediaItemRepository.Add(parameters.MediaItem);
            
            await _localStatisticsProvider.RefreshStatistics(parameters.FFprobePath, parameters.MediaItem);
            // TODO: reimplement this
            // await _localMetadataProvider.RefreshMetadata(parameters.MediaItem);
            // await _localPosterProvider.RefreshPoster(parameters.MediaItem);
            // await _smartCollectionBuilder.RefreshSmartCollections(parameters.MediaItem);

            return ProjectToViewModel(parameters.MediaItem);
        }

        private async Task<Validation<BaseError, RequestParameters>> Validate(CreateMediaItem request) =>
            (await ValidateMediaSource(request), PathMustExist(request), await ValidateFFprobePath())
            .Apply(
                (mediaSourceId, path, ffprobePath) => new RequestParameters(
                    ffprobePath,
                    new MediaItem
                    {
                        MediaSourceId = mediaSourceId,
                        Path = path
                    }));

        private async Task<Validation<BaseError, int>> ValidateMediaSource(CreateMediaItem createMediaItem) =>
            (await MediaSourceMustExist(createMediaItem)).Bind(MediaSourceMustBeLocal);

        private async Task<Validation<BaseError, MediaSource>> MediaSourceMustExist(CreateMediaItem createMediaItem) =>
            (await _mediaSourceRepository.Get(createMediaItem.MediaSourceId))
            .ToValidation<BaseError>($"[MediaSource] {createMediaItem.MediaSourceId} does not exist.");

        private Validation<BaseError, int> MediaSourceMustBeLocal(MediaSource mediaSource) =>
            Some(mediaSource)
                .Filter(ms => ms is LocalMediaSource)
                .ToValidation<BaseError>($"[MediaSource] {mediaSource.Id} must be a local media source")
                .Map(ms => ms.Id);

        private Validation<BaseError, string> PathMustExist(CreateMediaItem createMediaItem) =>
            Some(createMediaItem.Path)
                .Filter(File.Exists)
                .ToValidation<BaseError>("[Path] does not exist on the file system");

        private Task<Validation<BaseError, string>> ValidateFFprobePath() =>
            _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath)
                .FilterT(File.Exists)
                .Map(
                    ffprobePath =>
                        ffprobePath.ToValidation<BaseError>("FFprobe path does not exist on the file system"));

        private record RequestParameters(string FFprobePath, MediaItem MediaItem);
    }
}
