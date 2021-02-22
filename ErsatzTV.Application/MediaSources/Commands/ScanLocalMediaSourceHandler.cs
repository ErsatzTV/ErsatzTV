using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public class ScanLocalMediaSourceHandler : IRequestHandler<ForceScanLocalMediaSource, Either<BaseError, string>>,
        IRequestHandler<ScanLocalMediaSourceIfNeeded, Either<BaseError, string>>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly IEntityLocker _entityLocker;
        private readonly ILogger<ScanLocalMediaSourceHandler> _logger;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IMovieFolderScanner _movieFolderScanner;
        private readonly ITelevisionFolderScanner _televisionFolderScanner;

        public ScanLocalMediaSourceHandler(
            IMediaSourceRepository mediaSourceRepository,
            IConfigElementRepository configElementRepository,
            IMovieFolderScanner movieFolderScanner,
            ITelevisionFolderScanner televisionFolderScanner,
            IEntityLocker entityLocker,
            ILogger<ScanLocalMediaSourceHandler> logger)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _configElementRepository = configElementRepository;
            _movieFolderScanner = movieFolderScanner;
            _televisionFolderScanner = televisionFolderScanner;
            _entityLocker = entityLocker;
            _logger = logger;
        }

        public Task<Either<BaseError, string>> Handle(
            ForceScanLocalMediaSource request,
            CancellationToken cancellationToken) =>
            Handle((IScanLocalMediaSource) request, cancellationToken);

        public Task<Either<BaseError, string>> Handle(
            ScanLocalMediaSourceIfNeeded request,
            CancellationToken cancellationToken) =>
            Handle((IScanLocalMediaSource) request, cancellationToken);

        private Task<Either<BaseError, string>>
            Handle(IScanLocalMediaSource request, CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(parameters => PerformScan(parameters).Map(_ => parameters.LocalMediaSource.Folder))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> PerformScan(RequestParameters parameters)
        {
            DateTimeOffset lastScan = parameters.LocalMediaSource.LastScan ?? DateTime.MinValue;
            if (parameters.ForceScan || lastScan < DateTimeOffset.Now - TimeSpan.FromHours(6))
            {
                switch (parameters.LocalMediaSource.MediaType)
                {
                    case MediaType.Movie:
                        await _movieFolderScanner.ScanFolder(parameters.LocalMediaSource, parameters.FFprobePath);
                        break;
                    case MediaType.TvShow:
                        await _televisionFolderScanner.ScanFolder(parameters.LocalMediaSource, parameters.FFprobePath);
                        break;
                }

                parameters.LocalMediaSource.LastScan = DateTimeOffset.Now;
                await _mediaSourceRepository.Update(parameters.LocalMediaSource);
            }
            else
            {
                _logger.LogDebug(
                    "Skipping unforced scan of media source {Folder}",
                    parameters.LocalMediaSource.Folder);
            }

            _entityLocker.UnlockMediaSource(parameters.LocalMediaSource.Id);
            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>> Validate(IScanLocalMediaSource request) =>
            (await LocalMediaSourceMustExist(request), await ValidateFFprobePath())
            .Apply(
                (localMediaSource, ffprobePath) => new RequestParameters(
                    localMediaSource,
                    ffprobePath,
                    request.ForceScan));

        private Task<Validation<BaseError, LocalMediaSource>> LocalMediaSourceMustExist(
            IScanLocalMediaSource request) =>
            _mediaSourceRepository.Get(request.MediaSourceId)
                .Map(maybeMediaSource => maybeMediaSource.Map(ms => ms as LocalMediaSource))
                .Map(v => v.ToValidation<BaseError>($"Local media source {request.MediaSourceId} does not exist."));

        private Task<Validation<BaseError, string>> ValidateFFprobePath() =>
            _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath)
                .FilterT(File.Exists)
                .Map(
                    ffprobePath =>
                        ffprobePath.ToValidation<BaseError>("FFprobe path does not exist on the file system"));

        private record RequestParameters(LocalMediaSource LocalMediaSource, string FFprobePath, bool ForceScan);
    }
}
