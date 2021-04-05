using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application;
using ErsatzTV.Application.Search.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Services
{
    public class SearchIndexService : BackgroundService
    {
        private readonly ChannelReader<ISearchBackgroundServiceRequest> _channel;
        private readonly ILogger<SearchIndexService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SearchIndexService(
            ChannelReader<ISearchBackgroundServiceRequest> channel,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<SearchIndexService> logger)
        {
            _channel = channel;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Search index service started");

            await foreach (ISearchBackgroundServiceRequest request in _channel.ReadAllAsync(cancellationToken))
            {
                try
                {
                    using IServiceScope scope = _serviceScopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    switch (request)
                    {
                        case RebuildSearchIndex rebuildSearchIndex:
                            await mediator.Send(rebuildSearchIndex, cancellationToken);
                            break;
                        case AddItemsToSearchIndex addItemsToSearchIndex:
                            await mediator.Send(addItemsToSearchIndex, cancellationToken);
                            break;
                        case RemoveItemsFromSearchIndex removeItemsFromSearchIndex:
                            await mediator.Send(removeItemsFromSearchIndex, cancellationToken);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process search index service request");
                }
            }
        }
    }
}
