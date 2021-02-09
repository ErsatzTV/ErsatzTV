using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application;
using ErsatzTV.Application.MediaSources.Commands;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ErsatzTV.Services
{
    public class SchedulerService : IHostedService
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private Timer _timer;

        public SchedulerService(
            IServiceScopeFactory serviceScopeFactory,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _channel = channel;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(
                async _ => await DoWork(cancellationToken),
                null,
                TimeSpan.FromSeconds(0), // fire immediately
                TimeSpan.FromHours(1)); // repeat every hour

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {
            await BuildPlayouts(cancellationToken);
            await ScanLocalMediaSources(cancellationToken);
        }


        private async Task BuildPlayouts(CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

            List<int> playoutIds = await dbContext.Playouts.Map(p => p.Id).ToListAsync(cancellationToken);
            foreach (int playoutId in playoutIds)
            {
                await _channel.WriteAsync(new BuildPlayout(playoutId), cancellationToken);
            }
        }

        private async Task ScanLocalMediaSources(CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

            List<int> localMediaSourceIds = await dbContext.LocalMediaSources
                .Map(ms => ms.Id)
                .ToListAsync(cancellationToken);

            foreach (int mediaSourceId in localMediaSourceIds)
            {
                await _channel.WriteAsync(new ScanLocalMediaSource(mediaSourceId), cancellationToken);
            }
        }
    }
}
