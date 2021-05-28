using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public class
        SynchronizeJellyfinLibrariesHandler : MediatR.IRequestHandler<SynchronizeJellyfinLibraries,
            Either<BaseError, Unit>>

    {
        private readonly IJellyfinApiClient _jellyfinApiClient;
        private readonly IJellyfinSecretStore _jellyfinSecretStore;
        private readonly ILogger<SynchronizeJellyfinLibrariesHandler> _logger;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly ISearchIndex _searchIndex;

        public SynchronizeJellyfinLibrariesHandler(
            IMediaSourceRepository mediaSourceRepository,
            IJellyfinSecretStore jellyfinSecretStore,
            IJellyfinApiClient jellyfinApiClient,
            ILogger<SynchronizeJellyfinLibrariesHandler> logger,
            ISearchIndex searchIndex)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _jellyfinSecretStore = jellyfinSecretStore;
            _jellyfinApiClient = jellyfinApiClient;
            _logger = logger;
            _searchIndex = searchIndex;
        }

        public Task<Either<BaseError, Unit>> Handle(
            SynchronizeJellyfinLibraries request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(SynchronizeLibraries)
                .Bind(v => v.ToEitherAsync());

        private Task<Validation<BaseError, ConnectionParameters>> Validate(SynchronizeJellyfinLibraries request) =>
            MediaSourceMustExist(request)
                .BindT(MediaSourceMustHaveActiveConnection)
                .BindT(MediaSourceMustHaveApiKey);

        private Task<Validation<BaseError, JellyfinMediaSource>> MediaSourceMustExist(
            SynchronizeJellyfinLibraries request) =>
            _mediaSourceRepository.GetJellyfin(request.JellyfinMediaSourceId)
                .Map(o => o.ToValidation<BaseError>("Jellyfin media source does not exist."));

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

        private async Task<Unit> SynchronizeLibraries(ConnectionParameters connectionParameters)
        {
            Either<BaseError, List<JellyfinLibrary>> maybeLibraries = await _jellyfinApiClient.GetLibraries(
                connectionParameters.ActiveConnection.Address,
                connectionParameters.ApiKey);

            await maybeLibraries.Match(
                async libraries =>
                {
                    var existing = connectionParameters.JellyfinMediaSource.Libraries.OfType<JellyfinLibrary>()
                        .ToList();
                    var toAdd = libraries.Filter(library => existing.All(l => l.ItemId != library.ItemId)).ToList();
                    var toRemove = existing.Filter(library => libraries.All(l => l.ItemId != library.ItemId)).ToList();
                    List<int> ids = await _mediaSourceRepository.UpdateLibraries(
                        connectionParameters.JellyfinMediaSource.Id,
                        toAdd,
                        toRemove);
                    if (ids.Any())
                    {
                        await _searchIndex.RemoveItems(ids);
                        _searchIndex.Commit();
                    }
                },
                error =>
                {
                    _logger.LogWarning(
                        "Unable to synchronize libraries from jellyfin server {JellyfinServer}: {Error}",
                        connectionParameters.JellyfinMediaSource.ServerName,
                        error.Value);

                    return Task.CompletedTask;
                });

            return Unit.Default;
        }

        private record ConnectionParameters(
            JellyfinMediaSource JellyfinMediaSource,
            JellyfinConnection ActiveConnection)
        {
            public string ApiKey { get; set; }
        }
    }
}
