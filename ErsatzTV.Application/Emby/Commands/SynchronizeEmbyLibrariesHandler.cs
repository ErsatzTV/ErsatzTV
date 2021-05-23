using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Emby.Commands
{
    public class
        SynchronizeEmbyLibrariesHandler : MediatR.IRequestHandler<SynchronizeEmbyLibraries, Either<BaseError, Unit>>
    {
        private readonly IEmbyApiClient _embyApiClient;
        private readonly IEmbySecretStore _embySecretStore;
        private readonly ILogger<SynchronizeEmbyLibrariesHandler> _logger;
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public SynchronizeEmbyLibrariesHandler(
            IMediaSourceRepository mediaSourceRepository,
            IEmbySecretStore embySecretStore,
            IEmbyApiClient embyApiClient,
            ILogger<SynchronizeEmbyLibrariesHandler> logger)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _embySecretStore = embySecretStore;
            _embyApiClient = embyApiClient;
            _logger = logger;
        }

        public Task<Either<BaseError, Unit>> Handle(
            SynchronizeEmbyLibraries request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(SynchronizeLibraries)
                .Bind(v => v.ToEitherAsync());

        private Task<Validation<BaseError, ConnectionParameters>> Validate(SynchronizeEmbyLibraries request) =>
            MediaSourceMustExist(request)
                .BindT(MediaSourceMustHaveActiveConnection)
                .BindT(MediaSourceMustHaveApiKey);

        private Task<Validation<BaseError, EmbyMediaSource>> MediaSourceMustExist(
            SynchronizeEmbyLibraries request) =>
            _mediaSourceRepository.GetEmby(request.EmbyMediaSourceId)
                .Map(o => o.ToValidation<BaseError>("Emby media source does not exist."));

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

        private async Task<Unit> SynchronizeLibraries(ConnectionParameters connectionParameters)
        {
            Either<BaseError, List<EmbyLibrary>> maybeLibraries = await _embyApiClient.GetLibraries(
                connectionParameters.ActiveConnection.Address,
                connectionParameters.ApiKey);

            await maybeLibraries.Match(
                libraries =>
                {
                    var existing = connectionParameters.EmbyMediaSource.Libraries.OfType<EmbyLibrary>()
                        .ToList();
                    var toAdd = libraries.Filter(library => existing.All(l => l.ItemId != library.ItemId)).ToList();
                    var toRemove = existing.Filter(library => libraries.All(l => l.ItemId != library.ItemId)).ToList();
                    return _mediaSourceRepository.UpdateLibraries(
                        connectionParameters.EmbyMediaSource.Id,
                        toAdd,
                        toRemove);
                },
                error =>
                {
                    _logger.LogWarning(
                        "Unable to synchronize libraries from emby server {EmbyServer}: {Error}",
                        connectionParameters.EmbyMediaSource.ServerName,
                        error.Value);

                    return Task.CompletedTask;
                });

            return Unit.Default;
        }

        private record ConnectionParameters(
            EmbyMediaSource EmbyMediaSource,
            EmbyConnection ActiveConnection)
        {
            public string ApiKey { get; set; }
        }
    }
}
