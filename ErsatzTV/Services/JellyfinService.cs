using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using MediatR;

namespace ErsatzTV.Services;

public class JellyfinService : BackgroundService
{
    private readonly ChannelReader<IJellyfinBackgroundServiceRequest> _channel;
    private readonly ILogger<JellyfinService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SystemStartup _systemStartup;

    public JellyfinService(
        ChannelReader<IJellyfinBackgroundServiceRequest> channel,
        IServiceScopeFactory serviceScopeFactory,
        SystemStartup systemStartup,
        ILogger<JellyfinService> logger)
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
            if (!File.Exists(FileSystemLayout.JellyfinSecretsPath))
            {
                await File.WriteAllTextAsync(FileSystemLayout.JellyfinSecretsPath, "{}", stoppingToken);
            }

            _logger.LogInformation(
                "Jellyfin service started; secrets are at {JellyfinSecretsPath}",
                FileSystemLayout.JellyfinSecretsPath);

            // synchronize sources on startup
            await SynchronizeSources(new SynchronizeJellyfinMediaSources(), stoppingToken);

            await foreach (IJellyfinBackgroundServiceRequest request in _channel.ReadAllAsync(stoppingToken))
            {
                try
                {
                    Task requestTask;
                    switch (request)
                    {
                        case SynchronizeJellyfinMediaSources synchronizeJellyfinMediaSources:
                            requestTask = SynchronizeSources(synchronizeJellyfinMediaSources, stoppingToken);
                            break;
                        default:
                            throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}");
                    }

                    await requestTask;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process Jellyfin background service request");

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
            _logger.LogInformation("Jellyfin service shutting down");
        }
    }

    private async Task SynchronizeSources(
        SynchronizeJellyfinMediaSources request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Either<BaseError, List<JellyfinMediaSource>> result = await mediator.Send(request, cancellationToken);
        result.Match(
            sources =>
            {
                if (sources.Any())
                {
                    _logger.LogInformation("Successfully synchronized jellyfin media sources");
                }
            },
            error =>
            {
                _logger.LogWarning(
                    "Unable to synchronize jellyfin media sources: {Error}",
                    error.Value);
            });
    }
}
