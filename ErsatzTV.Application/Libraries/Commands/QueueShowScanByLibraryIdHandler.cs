using System.Threading.Channels;
using ErsatzTV.Application.Emby;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Application.Plex;
using ErsatzTV.Application.Subtitles;
using ErsatzTV.Core;
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
    ChannelWriter<IBackgroundServiceRequest> workerChannel,
    ILogger<QueueShowScanByLibraryIdHandler> logger)
    : IRequestHandler<QueueShowScanByLibraryId, bool>
{
    public async Task<bool> Handle(QueueShowScanByLibraryId request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Library> maybeLibrary = await dbContext.Libraries
            .AsNoTracking()
            .SelectOneAsync(l => l.Id, l => l.Id == request.LibraryId, cancellationToken);

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

            logger.LogDebug(
                "Queued show scan for library id {Id}, show: {ShowTitle}, deepScan: {DeepScan}",
                library.Id,
                request.ShowTitle,
                request.DeepScan);

            try
            {
                var success = false;
                switch (library)
                {
                    case PlexLibrary:
                        Either<BaseError, string> plexResult = await mediator.Send(
                            new SynchronizePlexShowById(library.Id, request.ShowId, request.DeepScan),
                            cancellationToken);
                        success = plexResult.IsRight;
                        break;
                    case JellyfinLibrary:
                        Either<BaseError, string> jellyfinResult = await mediator.Send(
                            new SynchronizeJellyfinShowById(library.Id, request.ShowId, request.DeepScan),
                            cancellationToken);
                        success = jellyfinResult.IsRight;
                        break;
                    case EmbyLibrary:
                        Either<BaseError, string> embyResult = await mediator.Send(
                            new SynchronizeEmbyShowById(library.Id, request.ShowId, request.DeepScan),
                            cancellationToken);
                        success = embyResult.IsRight;
                        break;
                    case LocalLibrary:
                        logger.LogWarning("Single show scanning is not supported for local libraries");
                        break;
                    default:
                        logger.LogWarning("Unknown library type for library {Id}", library.Id);
                        break;
                }

                if (success && request.DeepScan)
                {
                    await workerChannel.WriteAsync(new ExtractEmbeddedShowSubtitles(request.ShowId), cancellationToken);
                }

                return success;
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
