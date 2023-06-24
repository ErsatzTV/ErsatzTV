﻿using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Maintenance;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Application.Subtitles;
using ErsatzTV.Core;
using MediatR;

namespace ErsatzTV.Services;

public class WorkerService : BackgroundService
{
    private readonly ChannelReader<IBackgroundServiceRequest> _channel;
    private readonly ILogger<WorkerService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public WorkerService(
        ChannelReader<IBackgroundServiceRequest> channel,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<WorkerService> logger)
    {
        _channel = channel;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        
        try
        {
            _logger.LogInformation("Worker service started");

            await foreach (IBackgroundServiceRequest request in _channel.ReadAllAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                using IServiceScope scope = _serviceScopeFactory.CreateScope();

                try
                {
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    switch (request)
                    {
                        case RefreshChannelList refreshChannelList:
                            await mediator.Send(refreshChannelList, cancellationToken);
                            break;
                        case RefreshChannelData refreshChannelData:
                            await mediator.Send(refreshChannelData, cancellationToken);
                            break;
                        case BuildPlayout buildPlayout:
                            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                            var linkedTokenSource =
                                CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

                            Either<BaseError, Unit> buildPlayoutResult = await mediator.Send(
                                buildPlayout,
                                linkedTokenSource.Token);
                            buildPlayoutResult.BiIter(
                                _ => _logger.LogDebug("Built playout {PlayoutId}", buildPlayout.PlayoutId),
                                error => _logger.LogWarning(
                                    "Unable to build playout {PlayoutId}: {Error}",
                                    buildPlayout.PlayoutId,
                                    error.Value));
                            break;
                        case DeleteOrphanedArtwork deleteOrphanedArtwork:
                            await mediator.Send(deleteOrphanedArtwork, cancellationToken);
                            break;
                        case DeleteOrphanedSubtitles deleteOrphanedSubtitles:
                            await mediator.Send(deleteOrphanedSubtitles, cancellationToken);
                            break;
                        case AddTraktList addTraktList:
                            await mediator.Send(addTraktList, cancellationToken);
                            break;
                        case DeleteTraktList deleteTraktList:
                            await mediator.Send(deleteTraktList, cancellationToken);
                            break;
                        case MatchTraktListItems matchTraktListItems:
                            await mediator.Send(matchTraktListItems, cancellationToken);
                            break;
                        case ExtractEmbeddedSubtitles extractEmbeddedSubtitles:
                            await mediator.Send(extractEmbeddedSubtitles, cancellationToken);
                            break;
                        case ReleaseMemory aggressivelyReleaseMemory:
                            await mediator.Send(aggressivelyReleaseMemory, cancellationToken);
                            break;
                    }
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    // this can happen when we're shutting down
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process background service request");

                    try
                    {
                        IClient client = scope.ServiceProvider.GetRequiredService<IClient>();
                        client.Notify(ex);
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }
                }
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            _logger.LogInformation("Worker service shutting down");
        }
    }
}
