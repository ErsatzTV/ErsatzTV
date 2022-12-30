using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Plex;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using MediatR;

namespace ErsatzTV.Services;

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
        try
        {
            if (!File.Exists(FileSystemLayout.PlexSecretsPath))
            {
                await File.WriteAllTextAsync(FileSystemLayout.PlexSecretsPath, "{}", cancellationToken);
            }

            _logger.LogInformation(
                "Plex service started; secrets are at {PlexSecretsPath}",
                FileSystemLayout.PlexSecretsPath);

            // synchronize sources on startup
            await SynchronizeSources(new SynchronizePlexMediaSources(), cancellationToken);

            await foreach (IPlexBackgroundServiceRequest request in _channel.ReadAllAsync(cancellationToken))
            {
                try
                {
                    Task requestTask;
                    switch (request)
                    {
                        case TryCompletePlexPinFlow pinRequest:
                            requestTask = CompletePinFlow(pinRequest, cancellationToken);
                            break;
                        case SynchronizePlexMediaSources sourcesRequest:
                            requestTask = SynchronizeSources(sourcesRequest, cancellationToken);
                            break;
                        case SynchronizePlexLibraries synchronizePlexLibrariesRequest:
                            requestTask = SynchronizeLibraries(synchronizePlexLibrariesRequest, cancellationToken);
                            break;
                        case ISynchronizePlexLibraryById synchronizePlexLibraryById:
                            requestTask = SynchronizePlexLibrary(synchronizePlexLibraryById, cancellationToken);
                            break;
                        default:
                            throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}");
                    }

                    await requestTask;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process plex background service request");

                    try
                    {
                        using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                        {
                            IClient client = scope.ServiceProvider.GetRequiredService<IClient>();
                            client.Notify(ex);
                        }
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
            _logger.LogInformation("Plex service shutting down");
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
            success => _logger.LogInformation(
                success ? "Successfully authenticated with plex" : "Plex authentication timeout"),
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

    private async Task SynchronizePlexLibrary(
        ISynchronizePlexLibraryById request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        IEntityLocker entityLocker = scope.ServiceProvider.GetRequiredService<IEntityLocker>();

        Either<BaseError, string> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            name => _logger.LogDebug("Done synchronizing plex library {Name}", name),
            error => _logger.LogWarning(
                "Unable to synchronize plex library {LibraryId}: {Error}",
                request.PlexLibraryId,
                error.Value));
        
        if (entityLocker.IsLibraryLocked(request.PlexLibraryId))
        {
            entityLocker.UnlockLibrary(request.PlexLibraryId);
        }
    }
}
