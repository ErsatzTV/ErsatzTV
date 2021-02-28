using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Plex.Commands
{
    public class
        SynchronizePlexLibraryByIdHandler : IRequestHandler<ForceSynchronizePlexLibraryById, Either<BaseError, string>>,
            IRequestHandler<SynchronizePlexLibraryByIdIfNeeded, Either<BaseError, string>>
    {
        private readonly IEntityLocker _entityLocker;
        private readonly ILogger<SynchronizePlexLibraryByIdHandler> _logger;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IPlexMovieLibraryScanner _plexMovieLibraryScanner;
        private readonly IPlexSecretStore _plexSecretStore;

        public SynchronizePlexLibraryByIdHandler(
            IMediaSourceRepository mediaSourceRepository,
            IPlexSecretStore plexSecretStore,
            IPlexMovieLibraryScanner plexMovieLibraryScanner,
            IEntityLocker entityLocker,
            ILogger<SynchronizePlexLibraryByIdHandler> logger)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _plexSecretStore = plexSecretStore;
            _plexMovieLibraryScanner = plexMovieLibraryScanner;
            _entityLocker = entityLocker;
            _logger = logger;
        }

        public Task<Either<BaseError, string>> Handle(
            ForceSynchronizePlexLibraryById request,
            CancellationToken cancellationToken) => Handle(request);

        public Task<Either<BaseError, string>> Handle(
            SynchronizePlexLibraryByIdIfNeeded request,
            CancellationToken cancellationToken) => Handle(request);

        private Task<Either<BaseError, string>>
            Handle(ISynchronizePlexLibraryById request) =>
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
                        await _plexMovieLibraryScanner.ScanLibrary(
                            parameters.ConnectionParameters.ActiveConnection,
                            parameters.ConnectionParameters.PlexServerAuthToken,
                            parameters.Library);
                        break;
                    case LibraryMediaKind.Shows:
                        // TODO: plex tv scanner
                        // await _televisionFolderScanner.ScanFolder(parameters.LocalMediaSource, parameters.FFprobePath);
                        break;
                }

                parameters.Library.LastScan = DateTime.UtcNow;
                await _mediaSourceRepository.Update(parameters.Library);
            }
            else
            {
                _logger.LogDebug(
                    "Skipping unforced scan of plex media library {Name}",
                    parameters.Library.Name);
            }

            // _entityLocker.UnlockMediaSource(parameters.MediaSource.Id);
            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>> Validate(ISynchronizePlexLibraryById request) =>
            (await ValidateConnection(request), await PlexLibraryMustExist(request))
            .Apply(
                (connectionParameters, plexLibrary) => new RequestParameters(
                    connectionParameters,
                    plexLibrary,
                    request.ForceScan
                ));

        private Task<Validation<BaseError, ConnectionParameters>> ValidateConnection(
            ISynchronizePlexLibraryById request) =>
            PlexMediaSourceMustExist(request)
                .BindT(MediaSourceMustHaveActiveConnection)
                .BindT(MediaSourceMustHaveToken);

        private Task<Validation<BaseError, PlexMediaSource>> PlexMediaSourceMustExist(
            ISynchronizePlexLibraryById request) =>
            _mediaSourceRepository.GetPlex(request.PlexMediaSourceId)
                .Map(v => v.ToValidation<BaseError>($"Plex media source {request.PlexMediaSourceId} does not exist."));

        private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
            PlexMediaSource plexMediaSource)
        {
            Option<PlexConnection> maybeConnection =
                plexMediaSource.Connections.SingleOrDefault(c => c.IsActive);
            return maybeConnection.Map(connection => new ConnectionParameters(plexMediaSource, connection))
                .ToValidation<BaseError>("Plex media source requires an active connection");
        }

        private async Task<Validation<BaseError, ConnectionParameters>> MediaSourceMustHaveToken(
            ConnectionParameters connectionParameters)
        {
            Option<PlexServerAuthToken> maybeToken = await
                _plexSecretStore.GetServerAuthToken(connectionParameters.PlexMediaSource.ClientIdentifier);
            return maybeToken.Map(token => connectionParameters with { PlexServerAuthToken = token })
                .ToValidation<BaseError>("Plex media source requires a token");
        }

        private Task<Validation<BaseError, PlexLibrary>> PlexLibraryMustExist(
            ISynchronizePlexLibraryById request) =>
            _mediaSourceRepository.GetPlexLibrary(request.PlexLibraryId)
                .Map(v => v.ToValidation<BaseError>($"Plex library {request.PlexLibraryId} does not exist."));

        private record RequestParameters(
            ConnectionParameters ConnectionParameters,
            PlexLibrary Library,
            bool ForceScan);

        private record ConnectionParameters(
            PlexMediaSource PlexMediaSource,
            PlexConnection ActiveConnection)
        {
            public PlexServerAuthToken PlexServerAuthToken { get; set; }
        }
    }
}
