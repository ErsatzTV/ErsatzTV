using System.Globalization;
using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Plex;

public class SynchronizePlexMediaSourcesHandler : PlexBaseConnectionHandler, IRequestHandler<SynchronizePlexMediaSources,
    Either<BaseError, List<PlexMediaSource>>>
{
    private const string LocalhostUri = "http://localhost:32400";

    private readonly ChannelWriter<IScannerBackgroundServiceRequest> _channel;
    private readonly IEntityLocker _entityLocker;
    private readonly ILogger<SynchronizePlexMediaSourcesHandler> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IPlexSecretStore _plexSecretStore;
    private readonly IPlexServerApiClient _plexServerApiClient;
    private readonly IPlexTvApiClient _plexTvApiClient;

    public SynchronizePlexMediaSourcesHandler(
        IMediaSourceRepository mediaSourceRepository,
        IPlexTvApiClient plexTvApiClient,
        IPlexServerApiClient plexServerApiClient,
        IPlexSecretStore plexSecretStore,
        ChannelWriter<IScannerBackgroundServiceRequest> channel,
        IEntityLocker entityLocker,
        ILogger<SynchronizePlexMediaSourcesHandler> logger)
        : base(plexServerApiClient, mediaSourceRepository, logger)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _plexTvApiClient = plexTvApiClient;
        _plexServerApiClient = plexServerApiClient;
        _plexSecretStore = plexSecretStore;
        _channel = channel;
        _entityLocker = entityLocker;
        _logger = logger;
    }

    public Task<Either<BaseError, List<PlexMediaSource>>> Handle(
        SynchronizePlexMediaSources request,
        CancellationToken cancellationToken) => _plexTvApiClient.GetServers().BindAsync(SynchronizeAllServers);

    private async Task<Either<BaseError, List<PlexMediaSource>>> SynchronizeAllServers(
        List<PlexMediaSource> servers)
    {
        List<PlexMediaSource> allExisting = await _mediaSourceRepository.GetAllPlex();
        foreach (PlexMediaSource server in servers)
        {
            await SynchronizeServer(allExisting, server);
        }

        // delete removed servers
        foreach (PlexMediaSource removed in allExisting.Filter(
                     s => servers.All(pms => pms.ClientIdentifier != s.ClientIdentifier)))
        {
            _logger.LogWarning(
                "Deleting removed Plex server {ServerName}!",
                removed.Id.ToString(CultureInfo.InvariantCulture));
            await _mediaSourceRepository.DeletePlex(removed);
        }

        foreach (PlexMediaSource mediaSource in await _mediaSourceRepository.GetAllPlex())
        {
            await _channel.WriteAsync(new SynchronizePlexLibraries(mediaSource.Id));
        }

        _entityLocker.UnlockPlex();

        return allExisting;
    }

    private async Task SynchronizeServer(List<PlexMediaSource> allExisting, PlexMediaSource server)
    {
        if (server.Connections.All(c => c.Uri != LocalhostUri))
        {
            var localhost = new PlexConnection
            {
                PlexMediaSource = server,
                PlexMediaSourceId = server.Id,
                Uri = LocalhostUri
            };

            server.Connections.Add(localhost);
        }

        Option<PlexMediaSource> maybeExisting =
            allExisting.Find(s => s.ClientIdentifier == server.ClientIdentifier);

        foreach (PlexMediaSource existing in maybeExisting)
        {
            existing.Platform = server.Platform;
            existing.PlatformVersion = server.PlatformVersion;
            existing.ProductVersion = server.ProductVersion;
            existing.ServerName = server.ServerName;
            var toAdd = server.Connections
                .Filter(connection => existing.Connections.All(c => c.Uri != connection.Uri)).ToList();
            var toRemove = existing.Connections
                .Filter(connection => server.Connections.All(c => c.Uri != connection.Uri)).ToList();
            await _mediaSourceRepository.Update(existing, toAdd, toRemove);
            Option<PlexServerAuthToken> maybeToken = await _plexSecretStore.GetServerAuthToken(server.ClientIdentifier);
            if (maybeToken.IsNone)
            {
                _logger.LogError(
                    "Unable to activate Plex connection for server {Server} without auth token",
                    server.ServerName);
            }

            foreach (PlexServerAuthToken token in maybeToken)
            {
                await FindConnectionToActivate(existing, token);
            }
        }

        if (maybeExisting.IsNone)
        {
            await _mediaSourceRepository.Add(server);
            Option<PlexServerAuthToken> maybeToken = await _plexSecretStore.GetServerAuthToken(server.ClientIdentifier);
            if (maybeToken.IsNone)
            {
                _logger.LogError(
                    "Unable to activate Plex connection for server {Server} without auth token",
                    server.ServerName);
            }

            foreach (PlexServerAuthToken token in maybeToken)
            {
                await FindConnectionToActivate(server, token);
            }
        }
    }
}
