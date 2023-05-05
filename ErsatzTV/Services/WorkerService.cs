using System.Threading.Channels;
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
                            _logger.LogDebug("WorkerService - RefreshChannelList START");
                            await mediator.Send(refreshChannelList, cancellationToken);
                            _logger.LogDebug("WorkerService - RefreshChannelList FINISH");
                            break;
                        case RefreshChannelData refreshChannelData:
                            _logger.LogDebug("WorkerService - RefreshChannelData START");
                            await mediator.Send(refreshChannelData, cancellationToken);
                            _logger.LogDebug("WorkerService - RefreshChannelData FINISH");
                            break;
                        case BuildPlayout buildPlayout:
                            _logger.LogDebug("WorkerService - BuildPlayout START");
                            Either<BaseError, Unit> buildPlayoutResult = await mediator.Send(
                                buildPlayout,
                                cancellationToken);
                            buildPlayoutResult.BiIter(
                                _ => _logger.LogDebug("Built playout {PlayoutId}", buildPlayout.PlayoutId),
                                error => _logger.LogWarning(
                                    "Unable to build playout {PlayoutId}: {Error}",
                                    buildPlayout.PlayoutId,
                                    error.Value));
                            _logger.LogDebug("WorkerService - BuildPlayout FINISH");
                            break;
                        case DeleteOrphanedArtwork deleteOrphanedArtwork:
                            _logger.LogDebug("WorkerService - DeleteOrphanedArtwork START");
                            await mediator.Send(deleteOrphanedArtwork, cancellationToken);
                            _logger.LogDebug("WorkerService - DeleteOrphanedArtwork FINISH");
                            break;
                        case DeleteOrphanedSubtitles deleteOrphanedSubtitles:
                            _logger.LogDebug("WorkerService - DeleteOrphanedSubtitles START");
                            await mediator.Send(deleteOrphanedSubtitles, cancellationToken);
                            _logger.LogDebug("WorkerService - DeleteOrphanedSubtitles FINISH");
                            break;
                        case AddTraktList addTraktList:
                            _logger.LogDebug("WorkerService - AddTraktList START");
                            await mediator.Send(addTraktList, cancellationToken);
                            _logger.LogDebug("WorkerService - AddTraktList FINISH");
                            break;
                        case DeleteTraktList deleteTraktList:
                            _logger.LogDebug("WorkerService - DeleteTraktList START");
                            await mediator.Send(deleteTraktList, cancellationToken);
                            _logger.LogDebug("WorkerService - DeleteTraktList FINISH");
                            break;
                        case MatchTraktListItems matchTraktListItems:
                            _logger.LogDebug("WorkerService - MatchTraktListItems START");
                            await mediator.Send(matchTraktListItems, cancellationToken);
                            _logger.LogDebug("WorkerService - MatchTraktListItems FINISH");
                            break;
                        case ExtractEmbeddedSubtitles extractEmbeddedSubtitles:
                            _logger.LogDebug("WorkerService - ExtractEmbeddedSubtitles START");
                            await mediator.Send(extractEmbeddedSubtitles, cancellationToken);
                            _logger.LogDebug("WorkerService - ExtractEmbeddedSubtitles FINISH");
                            break;
                        case ReleaseMemory aggressivelyReleaseMemory:
                            _logger.LogDebug("WorkerService - ReleaseMemory START");
                            await mediator.Send(aggressivelyReleaseMemory, cancellationToken);
                            _logger.LogDebug("WorkerService - ReleaseMemory FINISH");
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
