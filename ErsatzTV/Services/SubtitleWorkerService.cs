using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Subtitles;
using MediatR;

namespace ErsatzTV.Services;

public class SubtitleWorkerService : BackgroundService
{
    private readonly ChannelReader<ISubtitleWorkerRequest> _channel;
    private readonly ILogger<SubtitleWorkerService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SubtitleWorkerService(
        ChannelReader<ISubtitleWorkerRequest> channel,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SubtitleWorkerService> logger)
    {
        _channel = channel;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Subtitle worker service started");

            await foreach (ISubtitleWorkerRequest request in _channel.ReadAllAsync(cancellationToken))
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();

                try
                {
                    switch (request)
                    {
                        case ExtractEmbeddedSubtitles extractEmbeddedSubtitles:
                            IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                            await mediator.Send(extractEmbeddedSubtitles, cancellationToken);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to handle subtitle worker request");

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
            _logger.LogInformation("Subtitle worker service shutting down");
        }
    }
}