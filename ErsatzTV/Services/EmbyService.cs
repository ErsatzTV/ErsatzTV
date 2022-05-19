using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Emby;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using MediatR;

namespace ErsatzTV.Services;

public class EmbyService : BackgroundService
{
    private readonly ChannelReader<IEmbyBackgroundServiceRequest> _channel;
    private readonly ILogger<EmbyService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EmbyService(
        ChannelReader<IEmbyBackgroundServiceRequest> channel,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<EmbyService> logger)
    {
        _channel = channel;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(FileSystemLayout.EmbySecretsPath))
            {
                await File.WriteAllTextAsync(FileSystemLayout.EmbySecretsPath, "{}", cancellationToken);
            }

            _logger.LogInformation(
                "Emby service started; secrets are at {EmbySecretsPath}",
                FileSystemLayout.EmbySecretsPath);

            // synchronize sources on startup
            await SynchronizeSources(new SynchronizeEmbyMediaSources(), cancellationToken);

            await foreach (IEmbyBackgroundServiceRequest request in _channel.ReadAllAsync(cancellationToken))
            {
                try
                {
                    Task requestTask;
                    switch (request)
                    {
                        case SynchronizeEmbyMediaSources synchronizeEmbyMediaSources:
                            requestTask = SynchronizeSources(synchronizeEmbyMediaSources, cancellationToken);
                            break;
                        case SynchronizeEmbyLibraries synchronizeEmbyLibraries:
                            requestTask = SynchronizeLibraries(synchronizeEmbyLibraries, cancellationToken);
                            break;
                        case ISynchronizeEmbyLibraryById synchronizeEmbyLibraryById:
                            requestTask = SynchronizeEmbyLibrary(synchronizeEmbyLibraryById, cancellationToken);
                            break;
                        default:
                            throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}");
                        case SynchronizeEmbyCollections synchronizeEmbyCollections:
                            requestTask = SynchronizeEmbyCollections(synchronizeEmbyCollections, cancellationToken);
                            break;
                    }

                    await requestTask;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process Emby background service request");

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
            _logger.LogInformation("Emby service shutting down");
        }
    }

    private async Task SynchronizeSources(SynchronizeEmbyMediaSources request, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Either<BaseError, List<EmbyMediaSource>> result = await mediator.Send(request, cancellationToken);
        result.Match(
            sources =>
            {
                if (sources.Any())
                {
                    _logger.LogInformation("Successfully synchronized emby media sources");
                }
            },
            error =>
            {
                _logger.LogWarning(
                    "Unable to synchronize emby media sources: {Error}",
                    error.Value);
            });
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

        Either<BaseError, string> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            name => _logger.LogDebug("Done synchronizing emby library {Name}", name),
            error => _logger.LogWarning(
                "Unable to synchronize emby library {LibraryId}: {Error}",
                request.EmbyLibraryId,
                error.Value));
    }

    private async Task SynchronizeEmbyCollections(
        SynchronizeEmbyCollections request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Either<BaseError, Unit> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            _ => _logger.LogDebug("Done synchronizing emby collections"),
            error => _logger.LogWarning(
                "Unable to synchronize emby collections for source {MediaSourceId}: {Error}",
                request.EmbyMediaSourceId,
                error.Value));
    }
}
