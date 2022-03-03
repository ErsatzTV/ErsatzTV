using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Plex;

public class
    SynchronizePlexMediaSourcesHandler : IRequestHandler<SynchronizePlexMediaSources,
        Either<BaseError, List<PlexMediaSource>>>
{
    private readonly ChannelWriter<IPlexBackgroundServiceRequest> _channel;
    private readonly IEntityLocker _entityLocker;
    private readonly ILogger<SynchronizePlexMediaSourcesHandler> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IPlexTvApiClient _plexTvApiClient;
    private readonly IPlexServerApiClient _plexServerApiClient;
    private readonly IPlexSecretStore _plexSecretStore;

    public SynchronizePlexMediaSourcesHandler(
        IMediaSourceRepository mediaSourceRepository,
        IPlexTvApiClient plexTvApiClient,
        IPlexServerApiClient plexServerApiClient,
        IPlexSecretStore plexSecretStore,
        ChannelWriter<IPlexBackgroundServiceRequest> channel,
        IEntityLocker entityLocker,
        ILogger<SynchronizePlexMediaSourcesHandler> logger)
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
            _logger.LogWarning("Deleting removed Plex server {ServerName}!", removed.Id.ToString());
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
            await FindConnectionToActivate(existing);
        }

        if (maybeExisting.IsNone)
        {
            await _mediaSourceRepository.Add(server);
            await FindConnectionToActivate(server);
        }
    }

    private async Task FindConnectionToActivate(PlexMediaSource server)
    {
        var prioritized = server.Connections.OrderBy(pc => pc.IsActive ? 0 : 1).ToList();
        foreach (PlexConnection connection in server.Connections)
        {
            connection.IsActive = false;
        }

        Option<PlexServerAuthToken> maybeToken = await _plexSecretStore.GetServerAuthToken(server.ClientIdentifier);
        foreach (PlexServerAuthToken token in maybeToken)
        {
            foreach (PlexConnection connection in prioritized)
            {
                try
                {
                    _logger.LogDebug("Attempting to locate to Plex at {Uri}", connection.Uri);
                    if (await _plexServerApiClient.Ping(connection, token))
                    {
                        _logger.LogInformation("Located Plex at {Uri}", connection.Uri);
                        connection.IsActive = true;
                        break;
                    }
                }
                catch
                {
                    // do nothing
                }
            }
        }

        if (maybeToken.IsNone)
        {
            _logger.LogError(
                "Unable to activate Plex connection for server {Server} without auth token",
                server.ServerName);
        }

        if (server.Connections.All(c => !c.IsActive))
        {
            _logger.LogError("Unable to locate Plex");
            server.Connections.Head().IsActive = true;
        }

        await _mediaSourceRepository.Update(server, new List<PlexConnection>(), new List<PlexConnection>());
    }
}