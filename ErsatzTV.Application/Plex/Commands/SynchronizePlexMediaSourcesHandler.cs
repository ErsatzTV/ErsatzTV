using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Plex.Commands
{
    public class
        SynchronizePlexMediaSourcesHandler : IRequestHandler<SynchronizePlexMediaSources,
            Either<BaseError, List<PlexMediaSource>>>
    {
        private readonly ChannelWriter<IPlexBackgroundServiceRequest> _channel;
        private readonly IEntityLocker _entityLocker;
        private readonly ILogger<SynchronizePlexMediaSourcesHandler> _logger;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IPlexTvApiClient _plexTvApiClient;

        public SynchronizePlexMediaSourcesHandler(
            IMediaSourceRepository mediaSourceRepository,
            IPlexTvApiClient plexTvApiClient,
            ChannelWriter<IPlexBackgroundServiceRequest> channel,
            IEntityLocker entityLocker,
            ILogger<SynchronizePlexMediaSourcesHandler> logger)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _plexTvApiClient = plexTvApiClient;
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

        private Task SynchronizeServer(List<PlexMediaSource> allExisting, PlexMediaSource server)
        {
            Option<PlexMediaSource> maybeExisting =
                allExisting.Find(s => s.ClientIdentifier == server.ClientIdentifier);
            return maybeExisting.Match(
                existing =>
                {
                    existing.Platform = server.Platform;
                    existing.PlatformVersion = server.PlatformVersion;
                    existing.ProductVersion = server.ProductVersion;
                    existing.ServerName = server.ServerName;
                    var toAdd = server.Connections
                        .Filter(connection => existing.Connections.All(c => c.Uri != connection.Uri)).ToList();
                    var toRemove = existing.Connections
                        .Filter(connection => server.Connections.All(c => c.Uri != connection.Uri)).ToList();
                    return _mediaSourceRepository.Update(existing, server.Connections, toAdd, toRemove);
                },
                async () =>
                {
                    if (server.Connections.Any())
                    {
                        server.Connections.Head().IsActive = true;
                    }

                    await _mediaSourceRepository.Add(server);
                });
        }
    }
}
