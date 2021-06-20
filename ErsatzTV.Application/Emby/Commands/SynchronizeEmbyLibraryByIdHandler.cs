using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Emby.Commands
{
    public class SynchronizeEmbyLibraryByIdHandler :
        IRequestHandler<ForceSynchronizeEmbyLibraryById, Either<BaseError, string>>,
        IRequestHandler<SynchronizeEmbyLibraryByIdIfNeeded, Either<BaseError, string>>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly IEmbyMovieLibraryScanner _embyMovieLibraryScanner;

        private readonly IEmbySecretStore _embySecretStore;
        private readonly IEmbyTelevisionLibraryScanner _embyTelevisionLibraryScanner;
        private readonly IEntityLocker _entityLocker;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILogger<SynchronizeEmbyLibraryByIdHandler> _logger;

        private readonly IMediaSourceRepository _mediaSourceRepository;

        public SynchronizeEmbyLibraryByIdHandler(
            IMediaSourceRepository mediaSourceRepository,
            IEmbySecretStore embySecretStore,
            IEmbyMovieLibraryScanner embyMovieLibraryScanner,
            IEmbyTelevisionLibraryScanner embyTelevisionLibraryScanner,
            ILibraryRepository libraryRepository,
            IEntityLocker entityLocker,
            IConfigElementRepository configElementRepository,
            ILogger<SynchronizeEmbyLibraryByIdHandler> logger)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _embySecretStore = embySecretStore;
            _embyMovieLibraryScanner = embyMovieLibraryScanner;
            _embyTelevisionLibraryScanner = embyTelevisionLibraryScanner;
            _libraryRepository = libraryRepository;
            _entityLocker = entityLocker;
            _configElementRepository = configElementRepository;
            _logger = logger;
        }

        public Task<Either<BaseError, string>> Handle(
            ForceSynchronizeEmbyLibraryById request,
            CancellationToken cancellationToken) => Handle(request);

        public Task<Either<BaseError, string>> Handle(
            SynchronizeEmbyLibraryByIdIfNeeded request,
            CancellationToken cancellationToken) => Handle(request);

        private Task<Either<BaseError, string>>
            Handle(ISynchronizeEmbyLibraryById request) =>
            Validate(request)
                .MapT(parameters => Synchronize(parameters).Map(_ => parameters.Library.Name))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> Synchronize(RequestParameters parameters)
        {
            var lastScan = new DateTimeOffset(parameters.Library.LastScan ?? DateTime.MinValue, TimeSpan.Zero);
            DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(parameters.LibraryRefreshInterval);
            if (parameters.ForceScan || nextScan < DateTimeOffset.Now)
            {
                switch (parameters.Library.MediaKind)
                {
                    case LibraryMediaKind.Movies:
                        await _embyMovieLibraryScanner.ScanLibrary(
                            parameters.ConnectionParameters.ActiveConnection.Address,
                            parameters.ConnectionParameters.ApiKey,
                            parameters.Library,
                            parameters.FFprobePath);
                        break;
                    case LibraryMediaKind.Shows:
                        await _embyTelevisionLibraryScanner.ScanLibrary(
                            parameters.ConnectionParameters.ActiveConnection.Address,
                            parameters.ConnectionParameters.ApiKey,
                            parameters.Library,
                            parameters.FFprobePath);
                        break;
                }

                parameters.Library.LastScan = DateTime.UtcNow;
                await _libraryRepository.UpdateLastScan(parameters.Library);
            }
            else
            {
                _logger.LogDebug(
                    "Skipping unforced scan of emby media library {Name}",
                    parameters.Library.Name);
            }

            _entityLocker.UnlockLibrary(parameters.Library.Id);
            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>> Validate(
            ISynchronizeEmbyLibraryById request) =>
            (await ValidateConnection(request), await EmbyLibraryMustExist(request),
                await ValidateLibraryRefreshInterval(), await ValidateFFprobePath())
            .Apply(
                (connectionParameters, embyLibrary, libraryRefreshInterval, ffprobePath) => new RequestParameters(
                    connectionParameters,
                    embyLibrary,
                    request.ForceScan,
                    libraryRefreshInterval,
                    ffprobePath
                ));

        private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
            ISynchronizeEmbyLibraryById request) =>
            EmbyMediaSourceMustExist(request)
                .BindT(MediaSourceMustHaveActiveConnection)
                .BindT(MediaSourceMustHaveApiKey);

        private Task<Validation<BaseError, EmbyMediaSource>> EmbyMediaSourceMustExist(
            ISynchronizeEmbyLibraryById request) =>
            _mediaSourceRepository.GetEmbyByLibraryId(request.EmbyLibraryId)
                .Map(
                    v => v.ToValidation<BaseError>(
                        $"Emby media source for library {request.EmbyLibraryId} does not exist."));

        private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
            EmbyMediaSource embyMediaSource)
        {
            Option<EmbyConnection> maybeConnection = embyMediaSource.Connections.HeadOrNone();
            return maybeConnection.Map(connection => new ConnectionParameters(embyMediaSource, connection))
                .ToValidation<BaseError>("Emby media source requires an active connection");
        }

        private async Task<Validation<BaseError, ConnectionParameters>> MediaSourceMustHaveApiKey(
            ConnectionParameters connectionParameters)
        {
            EmbySecrets secrets = await _embySecretStore.ReadSecrets();
            return Optional(secrets.Address == connectionParameters.ActiveConnection.Address)
                .Filter(match => match)
                .Map(_ => connectionParameters with { ApiKey = secrets.ApiKey })
                .ToValidation<BaseError>("Emby media source requires an api key");
        }

        private Task<Validation<BaseError, EmbyLibrary>> EmbyLibraryMustExist(
            ISynchronizeEmbyLibraryById request) =>
            _mediaSourceRepository.GetEmbyLibrary(request.EmbyLibraryId)
                .Map(v => v.ToValidation<BaseError>($"Emby library {request.EmbyLibraryId} does not exist."));

        private Task<Validation<BaseError, int>> ValidateLibraryRefreshInterval() =>
            _configElementRepository.GetValue<int>(ConfigElementKey.LibraryRefreshInterval)
                .FilterT(lri => lri > 0)
                .Map(lri => lri.ToValidation<BaseError>("Library refresh interval is invalid"));

        private Task<Validation<BaseError, string>> ValidateFFprobePath() =>
            _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath)
                .FilterT(File.Exists)
                .Map(
                    ffprobePath =>
                        ffprobePath.ToValidation<BaseError>("FFprobe path does not exist on the file system"));

        private record RequestParameters(
            ConnectionParameters ConnectionParameters,
            EmbyLibrary Library,
            bool ForceScan,
            int LibraryRefreshInterval,
            string FFprobePath);

        private record ConnectionParameters(
            EmbyMediaSource EmbyMediaSource,
            EmbyConnection ActiveConnection)
        {
            public string ApiKey { get; set; }
        }
    }
}
