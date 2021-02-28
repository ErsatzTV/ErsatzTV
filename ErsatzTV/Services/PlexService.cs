using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application;
using ErsatzTV.Application.Plex.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Services
{
    public class PlexService : BackgroundService
    {
        private readonly ChannelReader<IPlexBackgroundServiceRequest> _channel;
        private readonly ILogger<PlexService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public PlexService(
            ChannelReader<IPlexBackgroundServiceRequest> channel,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<PlexService> logger)
        {
            _channel = channel;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(FileSystemLayout.PlexSecretsPath))
            {
                await File.WriteAllTextAsync(FileSystemLayout.PlexSecretsPath, "{}", cancellationToken);
            }

            _logger.LogInformation(
                "Plex service started; secrets are at {PlexSecretsPath}",
                FileSystemLayout.PlexSecretsPath);

            // synchronize sources on startup
            List<PlexMediaSource> sources = await SynchronizeSources(
                new SynchronizePlexMediaSources(),
                cancellationToken);
            foreach (PlexMediaSource source in sources)
            {
                await SynchronizeLibraries(new SynchronizePlexLibraries(source.Id), cancellationToken);
            }

            await foreach (IPlexBackgroundServiceRequest request in _channel.ReadAllAsync(cancellationToken))
            {
                try
                {
                    Task requestTask = request switch
                    {
                        TryCompletePlexPinFlow pinRequest => CompletePinFlow(pinRequest, cancellationToken),
                        SynchronizePlexMediaSources sourcesRequest => SynchronizeSources(
                            sourcesRequest,
                            cancellationToken),
                        _ => throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}")
                    };

                    await requestTask;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process plex background service request");
                }
            }
        }

        private async Task<List<PlexMediaSource>> SynchronizeSources(
            SynchronizePlexMediaSources request,
            CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            Either<BaseError, List<PlexMediaSource>> result = await mediator.Send(request, cancellationToken);
            return result.Match(
                sources =>
                {
                    if (sources.Any())
                    {
                        _logger.LogInformation("Successfully synchronized plex media sources");
                    }

                    return sources;
                },
                error =>
                {
                    _logger.LogWarning(
                        "Unable to synchronize plex media sources: {Error}",
                        error.Value);
                    return new List<PlexMediaSource>();
                });
        }

        private async Task CompletePinFlow(
            TryCompletePlexPinFlow request,
            CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            Either<BaseError, bool> result = await mediator.Send(request, cancellationToken);
            result.BiIter(
                success =>
                {
                    if (success)
                    {
                        _logger.LogInformation("Successfully authenticated with plex");
                    }
                    else
                    {
                        _logger.LogInformation("Plex authentication timeout");
                    }
                },
                error => _logger.LogWarning("Unable to poll plex token: {Error}", error.Value));
        }

        private async Task SynchronizeLibraries(SynchronizePlexLibraries request, CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            Either<BaseError, Unit> result = await mediator.Send(request, cancellationToken);
            result.BiIter(
                _ => _logger.LogInformation(
                    "Successfully synchronized plex libraries for source {MediaSourceId}",
                    request.PlexMediaSourceId),
                error => _logger.LogWarning(
                    "Unable to synchronize plex libraries for source {MediaSourceId}: {Error}",
                    request.PlexMediaSourceId,
                    error.Value));
        }
    }
}
