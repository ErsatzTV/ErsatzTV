using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application;
using ErsatzTV.Application.Maintenance.Commands;
using ErsatzTV.Application.MediaCollections.Commands;
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
            DateTime firstRun = DateTime.Now;
            
            // run once immediately at startup
            if (!cancellationToken.IsCancellationRequested)
            {
                await DoWork(cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                int currentMinutes = DateTime.Now.TimeOfDay.Minutes;
                int toWait = currentMinutes < 30 ? 30 - currentMinutes : 60 - currentMinutes;
                _logger.LogDebug("Scheduler sleeping for {Minutes} minutes", toWait);
                await Task.Delay(TimeSpan.FromMinutes(toWait), cancellationToken);
                
                if (!cancellationToken.IsCancellationRequested)
                {
                    var roundedMinute = (int)(Math.Round(DateTime.Now.Minute / 5.0) * 5);
                    if (roundedMinute % 30 == 0)
                    {
                        // check for playouts to rebuild every 30 minutes
                        await RebuildPlayouts(cancellationToken);
                    }
                    if (roundedMinute % 60 == 0 && DateTime.Now.Subtract(firstRun) > TimeSpan.FromHours(1))
                    {
                        // do other work every hour (on the hour)
                        await DoWork(cancellationToken);
                    }
                }
            }
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {
            try
            {
                await DeleteOrphanedArtwork(cancellationToken);
                await RebuildSearchIndex(cancellationToken);
                await BuildPlayouts(cancellationToken);
                await ScanLocalMediaSources(cancellationToken);
                await ScanPlexMediaSources(cancellationToken);
                await MatchTraktLists(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during scheduler run");
            }
        }

        private async Task RebuildPlayouts(CancellationToken cancellationToken)
        {
            try
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

                List<Playout> playouts = await dbContext.Playouts
                    .Filter(p => p.DailyRebuildTime != null)
                    .Include(p => p.Channel)
                    .ToListAsync(cancellationToken);

                foreach (Playout playout in playouts.OrderBy(p => decimal.Parse(p.Channel.Number)))
                {
                    if (DateTime.Now.Subtract(DateTime.Today.Add(playout.DailyRebuildTime ?? TimeSpan.FromDays(7))) <
                        TimeSpan.FromMinutes(5))
                    {
                        await _workerChannel.WriteAsync(new BuildPlayout(playout.Id, true), cancellationToken);
                    }
                }
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

            List<Playout> playouts = await dbContext.Playouts
                .Include(p => p.Channel)
                .ToListAsync(cancellationToken);
            foreach (int playoutId in playouts.OrderBy(p => decimal.Parse(p.Channel.Number)).Map(p => p.Id))
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

        private async Task MatchTraktLists(CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

            List<TraktList> traktLists = await dbContext.TraktLists
                .ToListAsync(cancellationToken);

            if (traktLists.Any() && _entityLocker.LockTrakt())
            {
                TraktList last = traktLists.Last();
                foreach (TraktList list in traktLists)
                {
                    await _workerChannel.WriteAsync(
                        new MatchTraktListItems(list.Id, list == last),
                        cancellationToken);
                }
            }
        }

        private ValueTask RebuildSearchIndex(CancellationToken cancellationToken) =>
            _workerChannel.WriteAsync(new RebuildSearchIndex(), cancellationToken);

        private ValueTask DeleteOrphanedArtwork(CancellationToken cancellationToken) =>
            _workerChannel.WriteAsync(new DeleteOrphanedArtwork(), cancellationToken);
    }
}
