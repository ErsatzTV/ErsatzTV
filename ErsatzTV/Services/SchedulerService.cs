using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Emby;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Application.Maintenance;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaSources;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Application.Plex;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Services;

public class SchedulerService : BackgroundService
{
    private readonly ChannelWriter<IEmbyBackgroundServiceRequest> _embyWorkerChannel;
    private readonly IEntityLocker _entityLocker;
    private readonly ChannelWriter<IJellyfinBackgroundServiceRequest> _jellyfinWorkerChannel;
    private readonly ILogger<SchedulerService> _logger;
    private readonly ChannelWriter<IPlexBackgroundServiceRequest> _plexWorkerChannel;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

    public SchedulerService(
        IServiceScopeFactory serviceScopeFactory,
        ChannelWriter<IBackgroundServiceRequest> workerChannel,
        ChannelWriter<IPlexBackgroundServiceRequest> plexWorkerChannel,
        ChannelWriter<IJellyfinBackgroundServiceRequest> jellyfinWorkerChannel,
        ChannelWriter<IEmbyBackgroundServiceRequest> embyWorkerChannel,
        IEntityLocker entityLocker,
        ILogger<SchedulerService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _workerChannel = workerChannel;
        _plexWorkerChannel = plexWorkerChannel;
        _jellyfinWorkerChannel = jellyfinWorkerChannel;
        _embyWorkerChannel = embyWorkerChannel;
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

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(toWait), cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
            {
                // do nothing
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                var roundedMinute = (int)(Math.Round(DateTime.Now.Minute / 5.0) * 5);
                if (roundedMinute % 30 == 0)
                {
                    // check for playouts to reset every 30 minutes
                    await ResetPlayouts(cancellationToken);
                }

                if (roundedMinute % 60 == 0 && DateTime.Now.Subtract(firstRun) > TimeSpan.FromHours(1))
                {
                    // do other work every hour (on the hour)
                    await DoWork(cancellationToken);
                }
                else if (roundedMinute % 30 == 0)
                {
                    // release memory every 30 minutes no matter what
                    await ReleaseMemory(cancellationToken);
                }
            }
        }
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        try
        {
            await DeleteOrphanedArtwork(cancellationToken);
            await BuildPlayouts(cancellationToken);
#if !DEBUG_NO_SYNC
            await ScanLocalMediaSources(cancellationToken);
            await ScanPlexMediaSources(cancellationToken);
            await ScanJellyfinMediaSources(cancellationToken);
            await ScanEmbyMediaSources(cancellationToken);
#endif
            await MatchTraktLists(cancellationToken);

            await ReleaseMemory(cancellationToken);
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            // do nothing
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during scheduler run");

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

    private async Task ResetPlayouts(CancellationToken cancellationToken)
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
                DateTime now = DateTime.Now;
                DateTime target = DateTime.Today.Add(playout.DailyRebuildTime ?? TimeSpan.FromDays(7));
                // check absolute diff
                if (now.Subtract(target).Duration() < TimeSpan.FromMinutes(5))
                {
                    await _workerChannel.WriteAsync(
                        new BuildPlayout(playout.Id, PlayoutBuildMode.Reset),
                        cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during scheduler run");

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

    private async Task BuildPlayouts(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

        List<Playout> playouts = await dbContext.Playouts
            .Include(p => p.Channel)
            .ToListAsync(cancellationToken);
        foreach (int playoutId in playouts.OrderBy(p => decimal.Parse(p.Channel.Number)).Map(p => p.Id))
        {
            await _workerChannel.WriteAsync(
                new BuildPlayout(playoutId, PlayoutBuildMode.Continue),
                cancellationToken);
        }
    }

    private async Task ScanLocalMediaSources(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

        foreach (int libraryId in dbContext.LocalMediaSources.SelectMany(ms => ms.Libraries).Map(l => l.Id))
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

        foreach (PlexLibrary library in dbContext.PlexLibraries.Filter(l => l.ShouldSyncItems))
        {
            if (_entityLocker.LockLibrary(library.Id))
            {
                await _plexWorkerChannel.WriteAsync(
                    new SynchronizePlexLibraryByIdIfNeeded(library.Id),
                    cancellationToken);
            }
        }
    }

    private async Task ScanJellyfinMediaSources(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

        foreach (JellyfinLibrary library in dbContext.JellyfinLibraries.Filter(l => l.ShouldSyncItems))
        {
            if (_entityLocker.LockLibrary(library.Id))
            {
                await _jellyfinWorkerChannel.WriteAsync(
                    new SynchronizeJellyfinLibraryByIdIfNeeded(library.Id),
                    cancellationToken);
            }
        }
    }

    private async Task ScanEmbyMediaSources(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        TvContext dbContext = scope.ServiceProvider.GetRequiredService<TvContext>();

        foreach (EmbyLibrary library in dbContext.EmbyLibraries.Filter(l => l.ShouldSyncItems))
        {
            if (_entityLocker.LockLibrary(library.Id))
            {
                await _embyWorkerChannel.WriteAsync(
                    new SynchronizeEmbyLibraryByIdIfNeeded(library.Id),
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

    private ValueTask DeleteOrphanedArtwork(CancellationToken cancellationToken) =>
        _workerChannel.WriteAsync(new DeleteOrphanedArtwork(), cancellationToken);

    private ValueTask ReleaseMemory(CancellationToken cancellationToken) =>
        _workerChannel.WriteAsync(new ReleaseMemory(false), cancellationToken);
}
