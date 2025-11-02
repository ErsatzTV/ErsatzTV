using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Search;
using ErsatzTV.Core;
using MediatR;

namespace ErsatzTV.Services;

public class SearchIndexService : BackgroundService
{
    private readonly ChannelReader<ISearchIndexBackgroundServiceRequest> _channel;
    private readonly ILogger<SearchIndexService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SystemStartup _systemStartup;

    private const int MaxBatchSize = 100;
    private readonly TimeSpan _maxBatchTime = TimeSpan.FromSeconds(10);

    private enum SearchOperation { Reindex, Remove }

    public SearchIndexService(
        ChannelReader<ISearchIndexBackgroundServiceRequest> channel,
        IServiceScopeFactory serviceScopeFactory,
        SystemStartup systemStartup,
        ILogger<SearchIndexService> logger)
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
        try
        {
            _logger.LogInformation("Search index worker service started");

            var batch = new Dictionary<int, SearchOperation>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var firstRequest = await _channel.ReadAsync(stoppingToken);
                    AddRequestToBatch(firstRequest, batch);

                    using var timeoutCts = new CancellationTokenSource(_maxBatchTime);
                    using var linkedCts =
                        CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

                    try
                    {
                        while (batch.Count < MaxBatchSize && await _channel.WaitToReadAsync(linkedCts.Token))
                        {
                            if (_channel.TryRead(out var nextRequest))
                            {
                                AddRequestToBatch(nextRequest, batch);
                            }
                        }
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        // batch time expired.
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading from search index channel.");

                    // avoid fast-looping on error
                    await Task.Delay(1000, stoppingToken);
                }

                if (batch.Count > 0)
                {
                    await ProcessBatchAsync(batch, stoppingToken);
                    batch.Clear();
                }
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            _logger.LogInformation("Search index worker service shutting down");
        }
    }

    private static void AddRequestToBatch(
        ISearchIndexBackgroundServiceRequest request,
        IDictionary<int, SearchOperation> batch)
    {
        switch (request)
        {
            case ReindexMediaItems reindex:
                foreach (int id in reindex.MediaItemIds)
                {
                    batch[id] = SearchOperation.Reindex;
                }

                break;
            case RemoveMediaItems remove:
                foreach (int id in remove.MediaItemIds)
                {
                    batch[id] = SearchOperation.Remove;
                }

                break;
        }
    }

    private async Task ProcessBatchAsync(Dictionary<int, SearchOperation> batch, CancellationToken stoppingToken)
    {
        var idsToReindex = new List<int>();
        var idsToRemove = new List<int>();

        foreach ((int id, SearchOperation op) in batch)
        {
            switch (op)
            {
                case SearchOperation.Reindex:
                    idsToReindex.Add(id);
                    break;
                case SearchOperation.Remove:
                    idsToRemove.Add(id);
                    break;
            }
        }

        _logger.LogDebug(
            "Processing search index batch. Reindexing: {ReindexCount}, Removing: {RemoveCount}",
            idsToReindex.Count,
            idsToRemove.Count);

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            if (idsToRemove.Count > 0)
            {
                await mediator.Send(new RemoveMediaItems(idsToRemove), stoppingToken);
            }

            if (idsToReindex.Count > 0)
            {
                await mediator.Send(new ReindexMediaItems(idsToReindex), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to handle search index batch worker request");

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
