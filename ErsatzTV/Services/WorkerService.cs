using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Maintenance;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaSources;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Application.Search;
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
                        case BuildPlayout buildPlayout:
                            Either<BaseError, Unit> buildPlayoutResult = await mediator.Send(
                                buildPlayout,
                                cancellationToken);
                            buildPlayoutResult.BiIter(
                                _ => _logger.LogDebug("Built playout {PlayoutId}", buildPlayout.PlayoutId),
                                error => _logger.LogWarning(
                                    "Unable to build playout {PlayoutId}: {Error}",
                                    buildPlayout.PlayoutId,
                                    error.Value));
                            break;
                        case IScanLocalLibrary scanLocalLibrary:
#if !DEBUG_NO_SYNC
                            Either<BaseError, string> scanResult = await mediator.Send(
                                scanLocalLibrary,
                                cancellationToken);
                            scanResult.BiIter(
                                name => _logger.LogDebug(
                                    "Done scanning local library {Library}",
                                    name),
                                error => _logger.LogWarning(
                                    "Unable to scan local library {LibraryId}: {Error}",
                                    scanLocalLibrary.LibraryId,
                                    error.Value));
#endif
                            break;
                        case RebuildSearchIndex rebuildSearchIndex:
                            await mediator.Send(rebuildSearchIndex, cancellationToken);
                            break;
                        case DeleteOrphanedArtwork deleteOrphanedArtwork:
                            _logger.LogInformation("Deleting orphaned artwork from the database");
                            await mediator.Send(deleteOrphanedArtwork, cancellationToken);
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
