using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public class ScanLocalMediaSourceHandler : IRequestHandler<ScanLocalMediaSource, Either<BaseError, string>>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly ILocalMediaScanner _localMediaScanner;
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public ScanLocalMediaSourceHandler(
            IMediaSourceRepository mediaSourceRepository,
            IConfigElementRepository configElementRepository,
            ILocalMediaScanner localMediaScanner)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _configElementRepository = configElementRepository;
            _localMediaScanner = localMediaScanner;
        }

        public Task<Either<BaseError, string>>
            Handle(ScanLocalMediaSource request, CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(
                    p => _localMediaScanner.ScanLocalMediaSource(p.LocalMediaSource, p.FFprobePath, request.RefreshAllMetadata)
                        .Map(_ => p.LocalMediaSource.Folder))
                .Bind(v => v.ToEitherAsync());

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
