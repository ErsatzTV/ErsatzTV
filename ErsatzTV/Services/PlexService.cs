using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Plex;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using MediatR;

namespace ErsatzTV.Services;

public class PlexService : BackgroundService
{
    private readonly ChannelReader<IPlexBackgroundServiceRequest> _channel;
    private readonly ILogger<PlexService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SystemStartup _systemStartup;

    public PlexService(
        ChannelReader<IPlexBackgroundServiceRequest> channel,
        IServiceScopeFactory serviceScopeFactory,
        SystemStartup systemStartup,
        ILogger<PlexService> logger)
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
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            if (!File.Exists(FileSystemLayout.PlexSecretsPath))
            {
                await File.WriteAllTextAsync(FileSystemLayout.PlexSecretsPath, "{}", stoppingToken);
            }

            _logger.LogInformation(
                "Plex service started; secrets are at {PlexSecretsPath}",
                FileSystemLayout.PlexSecretsPath);

            // synchronize sources on startup
            await SynchronizeSources(new SynchronizePlexMediaSources(), stoppingToken);

            await foreach (IPlexBackgroundServiceRequest request in _channel.ReadAllAsync(stoppingToken))
            {
                try
                {
                    Task requestTask;
                    switch (request)
                    {
                        case TryCompletePlexPinFlow pinRequest:
                            requestTask = CompletePinFlow(pinRequest, stoppingToken);
                            break;
                        case SynchronizePlexMediaSources sourcesRequest:
                            requestTask = SynchronizeSources(sourcesRequest, stoppingToken);
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
        foreach (bool success in result.RightToSeq())
        {
            if (success)
            {
                _logger.LogInformation("Successfully authenticated with plex");
            }
            else
            {
                _logger.LogInformation("Plex authentication timeout");
            }
        }

        foreach (BaseError error in result.LeftToSeq())
        {
            _logger.LogWarning("Unable to poll plex token: {Error}", error.Value);
        }
    }
}
