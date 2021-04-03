using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application;
using ErsatzTV.Application.MediaSources.Commands;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Application.Plex.Commands;
using ErsatzTV.Application.Search.Commands;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Services
{
    public class SchedulerService : IHostedService
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IEntityLocker _entityLocker;
        private readonly ILogger<SchedulerService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private Timer _timer;

        public SchedulerService(
            IServiceScopeFactory serviceScopeFactory,
            ChannelWriter<IBackgroundServiceRequest> channel,
            IEntityLocker entityLocker,
            ILogger<SchedulerService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _channel = channel;
            _entityLocker = entityLocker;
            _logger = logger;
        }

        [SuppressMessage("ReSharper", "VSTHRD101")]
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
            try
            {
                await RebuildSearchIndex(cancellationToken);
                await BuildPlayouts(cancellationToken);
                await ScanLocalMediaSources(cancellationToken);
                await ScanPlexMediaSources(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during scheduler run");
            }
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

            List<int> localLibraryIds = await dbContext.LocalMediaSources
                .SelectMany(ms => ms.Libraries)
                .Map(l => l.Id)
                .ToListAsync(cancellationToken);

            foreach (int libraryId in localLibraryIds)
            {
                if (_entityLocker.LockLibrary(libraryId))
                {
                    await _channel.WriteAsync(
                        new ScanLocalLibraryIfNeeded(libraryId),
                        cancellationToken);
                }
            }
        }

        private async Task ScanPlexMediaSources(CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

            List<PlexLibrary> plexLibraries = await dbContext.PlexLibraries
                .Filter(l => l.ShouldSyncItems)
                .ToListAsync(cancellationToken);

            foreach (PlexLibrary library in plexLibraries)
            {
                if (_entityLocker.LockLibrary(library.Id))
                {
                    await _channel.WriteAsync(
                        new SynchronizePlexLibraryByIdIfNeeded(library.Id),
                        cancellationToken);
                }
            }
        }

        private async Task RebuildSearchIndex(CancellationToken cancellationToken) =>
            await _channel.WriteAsync(new RebuildSearchIndex(), cancellationToken);
    }
}
