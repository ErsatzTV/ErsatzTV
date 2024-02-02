using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Search;
using MediatR;

namespace ErsatzTV.Services;

public class SearchIndexService : BackgroundService
{
    private readonly ChannelReader<ISearchIndexBackgroundServiceRequest> _channel;
    private readonly ILogger<SearchIndexService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SearchIndexService(
        ChannelReader<ISearchIndexBackgroundServiceRequest> channel,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SearchIndexService> logger)
    {
        _channel = channel;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        try
        {
            _logger.LogInformation("Search index worker service started");

            await foreach (ISearchIndexBackgroundServiceRequest request in _channel.ReadAllAsync(stoppingToken))
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                try
                {
                    switch (request)
                    {
                        case ReindexMediaItems reindexMediaItems:
                            _logger.LogDebug("Reindexing media items: {MediaItemIds}", reindexMediaItems.MediaItemIds);
                            await mediator.Send(reindexMediaItems, stoppingToken);
                            break;
                        case RemoveMediaItems removeMediaItems:
                            _logger.LogDebug("Removing media items: {MediaItemIds}", removeMediaItems.MediaItemIds);
                            await mediator.Send(removeMediaItems, stoppingToken);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to handle search index worker request");

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
            _logger.LogInformation("Search index worker service shutting down");
        }
    }
}
