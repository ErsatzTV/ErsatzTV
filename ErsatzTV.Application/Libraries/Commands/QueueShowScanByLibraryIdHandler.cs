using ErsatzTV.Application.Emby;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Application.Plex;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Libraries;

public class QueueShowScanByLibraryIdHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IEntityLocker locker,
    IMediator mediator,
    ILogger<QueueShowScanByLibraryIdHandler> logger)
    : IRequestHandler<QueueShowScanByLibraryId, bool>
{
    public async Task<bool> Handle(QueueShowScanByLibraryId request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Library> maybeLibrary = await dbContext.Libraries
            .AsNoTracking()
            .SelectOneAsync(l => l.Id, l => l.Id == request.LibraryId);

        foreach (Library library in maybeLibrary)
        {
            bool shouldSyncItems = library switch
            {
                PlexLibrary plexLibrary => plexLibrary.ShouldSyncItems,
                JellyfinLibrary jellyfinLibrary => jellyfinLibrary.ShouldSyncItems,
                EmbyLibrary embyLibrary => embyLibrary.ShouldSyncItems,
                _ => true
            };

            if (!shouldSyncItems)
            {
                logger.LogWarning("Library sync is disabled for library id {Id}", library.Id);
                return false;
            }

            // Check if library is already being scanned - return false if locked
            if (!locker.LockLibrary(library.Id))
            {
                logger.LogWarning("Library {Id} is already being scanned, cannot scan individual show", library.Id);
                return false;
            }

            logger.LogDebug("Queued show scan for library id {Id}, show: {ShowTitle}, deepScan: {DeepScan}",
                library.Id, request.ShowTitle, request.DeepScan);

            try
            {
                switch (library)
                {
                    case PlexLibrary:
                        var plexResult = await mediator.Send(
                            new SynchronizePlexShowByTitle(library.Id, request.ShowTitle, request.DeepScan),
                            cancellationToken);
                        return plexResult.IsRight;
                    case JellyfinLibrary:
                        var jellyfinResult = await mediator.Send(
                            new SynchronizeJellyfinShowByTitle(library.Id, request.ShowTitle, request.DeepScan),
                            cancellationToken);
                        return jellyfinResult.IsRight;
                    case EmbyLibrary:
                        var embyResult = await mediator.Send(
                            new SynchronizeEmbyShowByTitle(library.Id, request.ShowTitle, request.DeepScan),
                            cancellationToken);
                        return embyResult.IsRight;
                    case LocalLibrary:
                        logger.LogWarning("Single show scanning is not supported for local libraries");
                        return false;
                    default:
                        logger.LogWarning("Unknown library type for library {Id}", library.Id);
                        return false;
                }
            }
            finally
            {
                // Always unlock the library when we're done
                locker.UnlockLibrary(library.Id);
            }
        }

        return false;
    }
}