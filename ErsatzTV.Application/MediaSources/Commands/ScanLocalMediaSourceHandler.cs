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
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public class ScanLocalMediaSourceHandler : IRequestHandler<ScanLocalMediaSource, Either<BaseError, string>>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly IEntityLocker _entityLocker;
        private readonly ILocalMediaScanner _localMediaScanner;
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public ScanLocalMediaSourceHandler(
            IMediaSourceRepository mediaSourceRepository,
            IConfigElementRepository configElementRepository,
            ILocalMediaScanner localMediaScanner,
            IEntityLocker entityLocker)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _configElementRepository = configElementRepository;
            _localMediaScanner = localMediaScanner;
            _entityLocker = entityLocker;
        }

        public Task<Either<BaseError, string>>
            Handle(ScanLocalMediaSource request, CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(parameters => PerformScan(request, parameters).Map(_ => parameters.LocalMediaSource.Folder))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> PerformScan(ScanLocalMediaSource request, RequestParameters parameters)
        {
            await _localMediaScanner.ScanLocalMediaSource(
                parameters.LocalMediaSource,
                parameters.FFprobePath,
                request.RefreshAllMetadata);

            _entityLocker.UnlockMediaSource(parameters.LocalMediaSource.Id);

            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>> Validate(ScanLocalMediaSource request) =>
            (await LocalMediaSourceMustExist(request), await ValidateFFprobePath())
            .Apply((localMediaSource, ffprobePath) => new RequestParameters(localMediaSource, ffprobePath));

        private Task<Validation<BaseError, LocalMediaSource>> LocalMediaSourceMustExist(
            ScanLocalMediaSource request) =>
            _mediaSourceRepository.Get(request.MediaSourceId)
                .Map(maybeMediaSource => maybeMediaSource.Map(ms => ms as LocalMediaSource))
                .Map(v => v.ToValidation<BaseError>($"Local media source {request.MediaSourceId} does not exist."));

        private Task<Validation<BaseError, string>> ValidateFFprobePath() =>
            _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath)
                .FilterT(File.Exists)
                .Map(
                    ffprobePath =>
                        ffprobePath.ToValidation<BaseError>("FFprobe path does not exist on the file system"));

        private record RequestParameters(LocalMediaSource LocalMediaSource, string FFprobePath);
    }
}
