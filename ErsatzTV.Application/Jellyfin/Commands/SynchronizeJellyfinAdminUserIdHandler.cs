using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public class
        SynchronizeJellyfinAdminUserIdHandler : MediatR.IRequestHandler<SynchronizeJellyfinAdminUserId,
            Either<BaseError, Unit>>
    {
        private readonly IJellyfinApiClient _jellyfinApiClient;
        private readonly IJellyfinSecretStore _jellyfinSecretStore;
        private readonly ILogger<SynchronizeJellyfinAdminUserIdHandler> _logger;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IMemoryCache _memoryCache;

        public SynchronizeJellyfinAdminUserIdHandler(
            IMemoryCache memoryCache,
            IMediaSourceRepository mediaSourceRepository,
            IJellyfinSecretStore jellyfinSecretStore,
            IJellyfinApiClient jellyfinApiClient,
            ILogger<SynchronizeJellyfinAdminUserIdHandler> logger)
        {
            _memoryCache = memoryCache;
            _mediaSourceRepository = mediaSourceRepository;
            _jellyfinSecretStore = jellyfinSecretStore;
            _jellyfinApiClient = jellyfinApiClient;
            _logger = logger;
        }

        public Task<Either<BaseError, Unit>> Handle(
            SynchronizeJellyfinAdminUserId request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .Map(v => v.ToEither<ConnectionParameters>())
                .BindT(PerformSync);

        private async Task<Either<BaseError, Unit>> PerformSync(ConnectionParameters parameters)
        {
            if (_memoryCache.TryGetValue(parameters.ActiveConnection, out string _))
            {
                return Unit.Default;
            }

            Either<BaseError, string> maybeUserId = await _jellyfinApiClient.GetAdminUserId(
                parameters.ActiveConnection.Address,
                parameters.ApiKey);

            return maybeUserId.Match<Either<BaseError, Unit>>(
                userId =>
                {
                    _logger.LogDebug("Jellyfin admin user id is {UserId}", userId);
                    _memoryCache.Set(parameters.ActiveConnection, userId);
                    return Unit.Default;
                },
                error => error);
        }

        private Task<Validation<BaseError, ConnectionParameters>> Validate(SynchronizeJellyfinAdminUserId request) =>
            MediaSourceMustExist(request)
                .BindT(MediaSourceMustHaveActiveConnection)
                .BindT(MediaSourceMustHaveApiKey);

        private Task<Validation<BaseError, JellyfinMediaSource>> MediaSourceMustExist(
            SynchronizeJellyfinAdminUserId request) =>
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

        private record ConnectionParameters(
            JellyfinMediaSource JellyfinMediaSource,
            JellyfinConnection ActiveConnection)
        {
            public string ApiKey { get; set; }
        }
    }
}
