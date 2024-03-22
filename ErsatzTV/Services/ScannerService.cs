using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Emby;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Application.MediaSources;
using ErsatzTV.Application.Plex;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Locking;
using MediatR;

namespace ErsatzTV.Services;

public class ScannerService : BackgroundService
{
    private readonly ChannelReader<IScannerBackgroundServiceRequest> _channel;
    private readonly ILogger<ScannerService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SystemStartup _systemStartup;

    public ScannerService(
        ChannelReader<IScannerBackgroundServiceRequest> channel,
        IServiceScopeFactory serviceScopeFactory,
        SystemStartup systemStartup,
        ILogger<ScannerService> logger)
    {
        _channel = channel;
        _serviceScopeFactory = serviceScopeFactory;
        _systemStartup = systemStartup;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        await _systemStartup.WaitForDatabase(stoppingToken);
        await _systemStartup.WaitForSearchIndex(stoppingToken);
        try
        {
            _logger.LogInformation("Scanner service started");

            await foreach (IScannerBackgroundServiceRequest request in _channel.ReadAllAsync(stoppingToken))
            {
                try
                {
                    Task requestTask;
                    switch (request)
                    {
                        case SynchronizePlexLibraries synchronizePlexLibraries:
                            requestTask = SynchronizeLibraries(synchronizePlexLibraries, stoppingToken);
                            break;
                        case ISynchronizePlexLibraryById synchronizePlexLibraryById:
                            requestTask = SynchronizePlexLibrary(synchronizePlexLibraryById, stoppingToken);
                            break;
                        case SynchronizePlexCollections synchronizePlexCollections:
                            requestTask = SynchronizePlexCollections(synchronizePlexCollections, stoppingToken);
                            break;
                        case SynchronizeJellyfinAdminUserId synchronizeJellyfinAdminUserId:
                            requestTask = SynchronizeAdminUserId(synchronizeJellyfinAdminUserId, stoppingToken);
                            break;
                        case SynchronizeJellyfinLibraries synchronizeJellyfinLibraries:
                            requestTask = SynchronizeLibraries(synchronizeJellyfinLibraries, stoppingToken);
                            break;
                        case ISynchronizeJellyfinLibraryById synchronizeJellyfinLibraryById:
                            requestTask = SynchronizeJellyfinLibrary(synchronizeJellyfinLibraryById, stoppingToken);
                            break;
                        case SynchronizeJellyfinCollections synchronizeJellyfinCollections:
                            requestTask = SynchronizeJellyfinCollections(synchronizeJellyfinCollections, stoppingToken);
                            break;
                        case SynchronizeEmbyLibraries synchronizeEmbyLibraries:
                            requestTask = SynchronizeLibraries(synchronizeEmbyLibraries, stoppingToken);
                            break;
                        case ISynchronizeEmbyLibraryById synchronizeEmbyLibraryById:
                            requestTask = SynchronizeEmbyLibrary(synchronizeEmbyLibraryById, stoppingToken);
                            break;
                        case SynchronizeEmbyCollections synchronizeEmbyCollections:
                            requestTask = SynchronizeEmbyCollections(synchronizeEmbyCollections, stoppingToken);
                            break;
                        case IScanLocalLibrary scanLocalLibrary:
                            requestTask = SynchronizeLocalLibrary(scanLocalLibrary, stoppingToken);
                            break;
                        default:
                            throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}");
                    }

                    await requestTask;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process scanner background service request");

                    try
                    {
                        using IServiceScope scope = _serviceScopeFactory.CreateScope();
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
            _logger.LogInformation("Scanner service shutting down");
        }
    }

    private async Task SynchronizeLocalLibrary(IScanLocalLibrary request, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        IEntityLocker entityLocker = scope.ServiceProvider.GetRequiredService<IEntityLocker>();

        Either<BaseError, string> scanResult = await mediator.Send(request, cancellationToken);
        scanResult.BiIter(
            name => _logger.LogDebug(
                "Done scanning local library {Library}",
                name),
            error =>
            {
                if (error is ScanIsNotRequired)
                {
                    _logger.LogDebug(
                        "Scan is not required for local library {LibraryId} at this time",
                        request.LibraryId);
                }
                else
                {
                    _logger.LogWarning(
                        "Unable to scan local library {LibraryId}: {Error}",
                        request.LibraryId,
                        error.Value);
                }
            });

        if (entityLocker.IsLibraryLocked(request.LibraryId))
        {
            entityLocker.UnlockLibrary(request.LibraryId);
        }
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
            error =>
            {
                if (error is ScanIsNotRequired)
                {
                    _logger.LogDebug(
                        "Scan is not required for plex library {LibraryId} at this time",
                        request.PlexLibraryId);
                }
                else
                {
                    _logger.LogWarning(
                        "Unable to synchronize plex library {LibraryId}: {Error}",
                        request.PlexLibraryId,
                        error.Value);
                }
            });

        if (entityLocker.IsLibraryLocked(request.PlexLibraryId))
        {
            entityLocker.UnlockLibrary(request.PlexLibraryId);
        }
    }

    private async Task SynchronizePlexCollections(
        SynchronizePlexCollections request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        IEntityLocker entityLocker = scope.ServiceProvider.GetRequiredService<IEntityLocker>();

        Either<BaseError, Unit> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            _ => _logger.LogDebug("Done synchronizing plex collections"),
            error =>
            {
                if (error is ScanIsNotRequired)
                {
                    _logger.LogDebug("Scan is not required for plex collections at this time");
                }
                else
                {
                    _logger.LogWarning("Unable to synchronize plex collections: {Error}", error.Value);
                }
            });

        if (entityLocker.ArePlexCollectionsLocked())
        {
            entityLocker.UnlockPlexCollections();
        }
    }

    private async Task SynchronizeAdminUserId(
        SynchronizeJellyfinAdminUserId request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Either<BaseError, Unit> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            _ => _logger.LogInformation(
                "Successfully synchronized Jellyfin admin user id for source {MediaSourceId}",
                request.JellyfinMediaSourceId),
            error => _logger.LogWarning(
                "Unable to synchronize Jellyfin admin user id for source {MediaSourceId}: {Error}",
                request.JellyfinMediaSourceId,
                error.Value));
    }

    private async Task SynchronizeLibraries(SynchronizeJellyfinLibraries request, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Either<BaseError, Unit> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            _ => _logger.LogInformation(
                "Successfully synchronized Jellyfin libraries for source {MediaSourceId}",
                request.JellyfinMediaSourceId),
            error => _logger.LogWarning(
                "Unable to synchronize Jellyfin libraries for source {MediaSourceId}: {Error}",
                request.JellyfinMediaSourceId,
                error.Value));
    }

    private async Task SynchronizeJellyfinLibrary(
        ISynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        IEntityLocker entityLocker = scope.ServiceProvider.GetRequiredService<IEntityLocker>();

        Either<BaseError, string> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            name => _logger.LogDebug("Done synchronizing jellyfin library {Name}", name),
            error =>
            {
                if (error is ScanIsNotRequired)
                {
                    _logger.LogDebug(
                        "Scan is not required for jellyfin library {LibraryId} at this time",
                        request.JellyfinLibraryId);
                }
                else
                {
                    _logger.LogWarning(
                        "Unable to synchronize jellyfin library {LibraryId}: {Error}",
                        request.JellyfinLibraryId,
                        error.Value);
                }
            });

        if (entityLocker.IsLibraryLocked(request.JellyfinLibraryId))
        {
            entityLocker.UnlockLibrary(request.JellyfinLibraryId);
        }
    }

    private async Task SynchronizeJellyfinCollections(
        SynchronizeJellyfinCollections request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        IEntityLocker entityLocker = scope.ServiceProvider.GetRequiredService<IEntityLocker>();

        Either<BaseError, Unit> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            _ => _logger.LogDebug("Done synchronizing jellyfin collections"),
            error =>
            {
                if (error is ScanIsNotRequired)
                {
                    _logger.LogDebug("Scan is not required for jellyfin collections at this time");
                }
                else
                {
                    _logger.LogWarning("Unable to synchronize jellyfin collections: {Error}", error.Value);
                }
            });

        if (entityLocker.AreJellyfinCollectionsLocked())
        {
            entityLocker.UnlockJellyfinCollections();
        }
    }

    private async Task SynchronizeLibraries(SynchronizeEmbyLibraries request, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Either<BaseError, Unit> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            _ => _logger.LogInformation(
                "Successfully synchronized Emby libraries for source {MediaSourceId}",
                request.EmbyMediaSourceId),
            error => _logger.LogWarning(
                "Unable to synchronize Emby libraries for source {MediaSourceId}: {Error}",
                request.EmbyMediaSourceId,
                error.Value));
    }

    private async Task SynchronizeEmbyLibrary(ISynchronizeEmbyLibraryById request, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        IEntityLocker entityLocker = scope.ServiceProvider.GetRequiredService<IEntityLocker>();

        Either<BaseError, string> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            name => _logger.LogDebug("Done synchronizing emby library {Name}", name),
            error =>
            {
                if (error is ScanIsNotRequired)
                {
                    _logger.LogDebug(
                        "Scan is not required for emby library {LibraryId} at this time",
                        request.EmbyLibraryId);
                }
                else
                {
                    _logger.LogWarning(
                        "Unable to synchronize emby library {LibraryId}: {Error}",
                        request.EmbyLibraryId,
                        error.Value);
                }
            });

        if (entityLocker.IsLibraryLocked(request.EmbyLibraryId))
        {
            entityLocker.UnlockLibrary(request.EmbyLibraryId);
        }
    }

    private async Task SynchronizeEmbyCollections(
        SynchronizeEmbyCollections request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        IEntityLocker entityLocker = scope.ServiceProvider.GetRequiredService<IEntityLocker>();

        Either<BaseError, Unit> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            _ => _logger.LogDebug("Done synchronizing emby collections"),
            error =>
            {
                if (error is ScanIsNotRequired)
                {
                    _logger.LogDebug("Scan is not required for emby collections at this time");
                }
                else
                {
                    _logger.LogWarning("Unable to synchronize emby collections: {Error}", error.Value);
                }
            });

        if (entityLocker.AreEmbyCollectionsLocked())
        {
            entityLocker.UnlockEmbyCollections();
        }
    }
}
