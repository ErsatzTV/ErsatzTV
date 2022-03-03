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

namespace ErsatzTV.Application.Jellyfin;

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
        if (_memoryCache.TryGetValue($"jellyfin_admin_user_id.{parameters.JellyfinMediaSource.Id}", out string _))
        {
            return Unit.Default;
        }

        Either<BaseError, string> maybeUserId = await _jellyfinApiClient.GetAdminUserId(
            parameters.ActiveConnection.Address,
            parameters.ApiKey);

        return await maybeUserId.Match(
            userId =>
            {
                // _logger.LogDebug("Jellyfin admin user id is {UserId}", userId);
                _memoryCache.Set($"jellyfin_admin_user_id.{parameters.JellyfinMediaSource.Id}", userId);
                return Task.FromResult<Either<BaseError, Unit>>(Unit.Default);
            },
            async error =>
            {
                // clear api key if unable to sync with jellyfin
                if (error.Value.Contains("Unauthorized"))
                {
                    await _jellyfinSecretStore.SaveSecrets(
                        new JellyfinSecrets { Address = parameters.ActiveConnection.Address, ApiKey = null });
                }

                return Left<BaseError, Unit>(error);
            });
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
            .Where(match => match)
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