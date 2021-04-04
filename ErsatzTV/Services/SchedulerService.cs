using System;
using System.Collections.Generic;
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
    public class SchedulerService : BackgroundService
    {
        private readonly IEntityLocker _entityLocker;
        private readonly ILogger<SchedulerService> _logger;
        private readonly ChannelWriter<IPlexBackgroundServiceRequest> _plexWorkerChannel;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

        public SchedulerService(
            IServiceScopeFactory serviceScopeFactory,
            ChannelWriter<IBackgroundServiceRequest> workerChannel,
            ChannelWriter<IPlexBackgroundServiceRequest> plexWorkerChannel,
            IEntityLocker entityLocker,
            ILogger<SchedulerService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _workerChannel = workerChannel;
            _plexWorkerChannel = plexWorkerChannel;
            _entityLocker = entityLocker;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await DoWork(cancellationToken);
                }

                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
            }
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
                await _workerChannel.WriteAsync(new BuildPlayout(playoutId), cancellationToken);
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
                    await _workerChannel.WriteAsync(
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
                    await _plexWorkerChannel.WriteAsync(
                        new SynchronizePlexLibraryByIdIfNeeded(library.Id),
                        cancellationToken);
                }
            }
        }

        private ValueTask RebuildSearchIndex(CancellationToken cancellationToken) =>
            _workerChannel.WriteAsync(new RebuildSearchIndex(), cancellationToken);
    }
}
