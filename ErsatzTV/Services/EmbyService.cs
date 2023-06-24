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
    private readonly SystemStartup _systemStartup;

    public EmbyService(
        ChannelReader<IEmbyBackgroundServiceRequest> channel,
        IServiceScopeFactory serviceScopeFactory,
        SystemStartup systemStartup,
        ILogger<EmbyService> logger)
    {
        _channel = channel;
        _serviceScopeFactory = serviceScopeFactory;
        _systemStartup = systemStartup;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        
        await _systemStartup.WaitForDatabase(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

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
                        default:
                            throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}");
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
}
