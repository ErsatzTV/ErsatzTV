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
        RefreshMediaItemStatisticsHandler : MediatR.IRequestHandler<RefreshMediaItemStatistics, Either<BaseError, Unit>>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly ILocalStatisticsProvider _localStatisticsProvider;
        private readonly IMediaItemRepository _mediaItemRepository;

        public RefreshMediaItemStatisticsHandler(
            IMediaItemRepository mediaItemRepository,
            IConfigElementRepository configElementRepository,
            ILocalStatisticsProvider localStatisticsProvider)
        {
            _mediaItemRepository = mediaItemRepository;
            _configElementRepository = configElementRepository;
            _localStatisticsProvider = localStatisticsProvider;
        }

        public Task<Either<BaseError, Unit>> Handle(
            RefreshMediaItemStatistics request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(RefreshStatistics)
                .Bind(v => v.ToEitherAsync());

        private async Task<Validation<BaseError, RefreshParameters>> Validate(RefreshMediaItemStatistics request) =>
            (await MediaItemMustExist(request).BindT(PathMustExist), await ValidateFFprobePath())
            .Apply((mediaItem, ffprobePath) => new RefreshParameters(mediaItem, ffprobePath));

        private Task<Validation<BaseError, MediaItem>> MediaItemMustExist(
            RefreshMediaItemStatistics refreshMediaItemStatistics) =>
            _mediaItemRepository.Get(refreshMediaItemStatistics.MediaItemId)
                .Map(
                    maybeItem => maybeItem.ToValidation<BaseError>(
                        $"[MediaItem] {refreshMediaItemStatistics.MediaItemId} does not exist."));

        private Validation<BaseError, MediaItem> PathMustExist(MediaItem mediaItem) =>
            Some(mediaItem)
                .Filter(item => File.Exists(item.Path))
                .ToValidation<BaseError>($"[Path] '{mediaItem.Path}' does not exist on the file system");

        private Task<Validation<BaseError, string>> ValidateFFprobePath() =>
            _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath)
                .FilterT(File.Exists)
                .Map(
                    ffprobePath =>
                        ffprobePath.ToValidation<BaseError>("FFprobe path does not exist on the file system"));

        private Task<Unit> RefreshStatistics(RefreshParameters parameters) =>
            _localStatisticsProvider.RefreshStatistics(parameters.FFprobePath, parameters.MediaItem).ToUnit();

        private record RefreshParameters(MediaItem MediaItem, string FFprobePath);
    }
}
