using System;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public class
        SynchronizeJellyfinLibraryByIdHandler :
            IRequestHandler<ForceSynchronizeJellyfinLibraryById, Either<BaseError, string>>,
            IRequestHandler<SynchronizeJellyfinLibraryByIdIfNeeded, Either<BaseError, string>>
    {
        private readonly IEntityLocker _entityLocker;
        private readonly IJellyfinMovieLibraryScanner _jellyfinMovieLibraryScanner;

        private readonly IJellyfinSecretStore _jellyfinSecretStore;
        private readonly IJellyfinTelevisionLibraryScanner _jellyfinTelevisionLibraryScanner;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILogger<SynchronizeJellyfinLibraryByIdHandler> _logger;

        private readonly IMediaSourceRepository _mediaSourceRepository;

        public SynchronizeJellyfinLibraryByIdHandler(
            IMediaSourceRepository mediaSourceRepository,
            IJellyfinSecretStore jellyfinSecretStore,
            IJellyfinMovieLibraryScanner jellyfinMovieLibraryScanner,
            IJellyfinTelevisionLibraryScanner jellyfinTelevisionLibraryScanner,
            ILibraryRepository libraryRepository,
            IEntityLocker entityLocker,
            ILogger<SynchronizeJellyfinLibraryByIdHandler> logger)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _jellyfinSecretStore = jellyfinSecretStore;
            _jellyfinMovieLibraryScanner = jellyfinMovieLibraryScanner;
            _jellyfinTelevisionLibraryScanner = jellyfinTelevisionLibraryScanner;
            _libraryRepository = libraryRepository;
            _entityLocker = entityLocker;
            _logger = logger;
        }

        public Task<Either<BaseError, string>> Handle(
            ForceSynchronizeJellyfinLibraryById request,
            CancellationToken cancellationToken) => Handle(request);

        public Task<Either<BaseError, string>> Handle(
            SynchronizeJellyfinLibraryByIdIfNeeded request,
            CancellationToken cancellationToken) => Handle(request);

        private Task<Either<BaseError, string>>
            Handle(ISynchronizeJellyfinLibraryById request) =>
            Validate(request)
                .MapT(parameters => Synchronize(parameters).Map(_ => parameters.Library.Name))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> Synchronize(RequestParameters parameters)
        {
            var lastScan = new DateTimeOffset(parameters.Library.LastScan ?? DateTime.MinValue, TimeSpan.Zero);
            if (parameters.ForceScan || lastScan < DateTimeOffset.Now - TimeSpan.FromHours(6))
            {
                switch (parameters.Library.MediaKind)
                {
                    case LibraryMediaKind.Movies:
                        await _jellyfinMovieLibraryScanner.ScanLibrary(
                            parameters.ConnectionParameters.ActiveConnection.Address,
                            parameters.ConnectionParameters.ApiKey,
                            parameters.Library);
                        break;
                    case LibraryMediaKind.Shows:
                        await _jellyfinTelevisionLibraryScanner.ScanLibrary(
                            parameters.ConnectionParameters.ActiveConnection.Address,
                            parameters.ConnectionParameters.ApiKey,
                            parameters.Library);
                        break;
                }

                parameters.Library.LastScan = DateTime.UtcNow;
                await _libraryRepository.UpdateLastScan(parameters.Library);
            }
            else
            {
                _logger.LogDebug(
                    "Skipping unforced scan of jellyfin media library {Name}",
                    parameters.Library.Name);
            }

            _entityLocker.UnlockLibrary(parameters.Library.Id);
            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>> Validate(
            ISynchronizeJellyfinLibraryById request) =>
            (await ValidateConnection(request), await JellyfinLibraryMustExist(request))
            .Apply(
                (connectionParameters, jellyfinLibrary) => new RequestParameters(
                    connectionParameters,
                    jellyfinLibrary,
                    request.ForceScan
                ));

        private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
            ISynchronizeJellyfinLibraryById request) =>
            JellyfinMediaSourceMustExist(request)
                .BindT(MediaSourceMustHaveActiveConnection)
                .BindT(MediaSourceMustHaveApiKey);

        private Task<Validation<BaseError, JellyfinMediaSource>> JellyfinMediaSourceMustExist(
            ISynchronizeJellyfinLibraryById request) =>
            _mediaSourceRepository.GetJellyfinByLibraryId(request.JellyfinLibraryId)
                .Map(
                    v => v.ToValidation<BaseError>(
                        $"Jellyfin media source for library {request.JellyfinLibraryId} does not exist."));

        private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
            JellyfinMediaSource jellyfinMediaSource)
        {
            Option<JellyfinConnection> maybeConnection = jellyfinMediaSource.Connections.HeadOrNone();
            return maybeConnection.Map(connection => new ConnectionParameters(jellyfinMediaSource, connection))
                .ToValidation<BaseError>("Jellyfin media source requires an active connection");
        }

        private async Task<Validation<BaseError, ConnectionParameters>> MediaSourceMustHaveApiKey(
            ConnectionParameters connectionParameters)
        {
            JellyfinSecrets secrets = await _jellyfinSecretStore.ReadSecrets();
            return Optional(secrets.Address == connectionParameters.ActiveConnection.Address)
                .Filter(match => match)
                .Map(_ => connectionParameters with { ApiKey = secrets.ApiKey })
                .ToValidation<BaseError>("Jellyfin media source requires an api key");
        }

        private Task<Validation<BaseError, JellyfinLibrary>> JellyfinLibraryMustExist(
            ISynchronizeJellyfinLibraryById request) =>
            _mediaSourceRepository.GetJellyfinLibrary(request.JellyfinLibraryId)
                .Map(v => v.ToValidation<BaseError>($"Jellyfin library {request.JellyfinLibraryId} does not exist."));

        private record RequestParameters(
            ConnectionParameters ConnectionParameters,
            JellyfinLibrary Library,
            bool ForceScan);

        private record ConnectionParameters(
            JellyfinMediaSource JellyfinMediaSource,
            JellyfinConnection ActiveConnection)
        {
            public string ApiKey { get; set; }
        }
    }
}
