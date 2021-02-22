using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application;
using ErsatzTV.Application.MediaSources.Commands;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Services
{
    public class WorkerService : BackgroundService
    {
        private readonly ChannelReader<IBackgroundServiceRequest> _channel;
        private readonly ILogger<WorkerService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public WorkerService(
            ChannelReader<IBackgroundServiceRequest> channel,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<WorkerService> logger)
        {
            _channel = channel;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker service started");

            await foreach (IBackgroundServiceRequest request in _channel.ReadAllAsync(cancellationToken))
            {
                try
                {
                    using IServiceScope scope = _serviceScopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    switch (request)
                    {
                        case BuildPlayout buildPlayout:
                            Either<BaseError, Unit> buildPlayoutResult = await mediator.Send(
                                buildPlayout,
                                cancellationToken);
                            buildPlayoutResult.BiIter(
                                _ => _logger.LogDebug("Built playout {PlayoutId}", buildPlayout.PlayoutId),
                                error => _logger.LogWarning(
                                    "Unable to build playout {PlayoutId}: {Error}",
                                    buildPlayout.PlayoutId,
                                    error.Value));
                            break;
                        case IScanLocalMediaSource scanLocalMediaSource:
                            Either<BaseError, string> scanResult = await mediator.Send(
                                scanLocalMediaSource,
                                cancellationToken);
                            scanResult.BiIter(
                                name => _logger.LogDebug(
                                    "Done scanning local media source {MediaSource}",
                                    name),
                                error => _logger.LogWarning(
                                    "Unable to scan local media source {MediaSourceId}: {Error}",
                                    scanLocalMediaSource.MediaSourceId,
                                    error.Value));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process background service request");
                }
            }
        }
    }
}
