using System.Threading.Channels;
using ErsatzTV.Application.Emby;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Application.MediaSources;
using ErsatzTV.Application.Plex;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Libraries;

public class QueueLibraryScanByLibraryIdHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IEntityLocker locker,
    ChannelWriter<IScannerBackgroundServiceRequest> scannerWorker,
    ILogger<QueueLibraryScanByLibraryIdHandler> logger)
    : IRequestHandler<QueueLibraryScanByLibraryId, bool>
{
    public async Task<bool> Handle(QueueLibraryScanByLibraryId request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Library> maybeLibrary = await dbContext.Libraries
            .AsNoTracking()
            .SelectOneAsync(l => l.Id, l => l.Id == request.LibraryId);

        foreach (Library library in maybeLibrary)
        {
            if (locker.LockLibrary(library.Id))
            {
                logger.LogDebug("Queued library scan for library id {Id}", library.Id);

                switch (library)
                {
                    case LocalLibrary:
                        await scannerWorker.WriteAsync(new ForceScanLocalLibrary(library.Id), cancellationToken);
                        break;
                    case PlexLibrary:
                        await scannerWorker.WriteAsync(
                            new SynchronizePlexLibraries(library.MediaSourceId),
                            cancellationToken);
                        await scannerWorker.WriteAsync(
                            new ForceSynchronizePlexLibraryById(library.Id, false),
                            cancellationToken);
                        break;
                    case JellyfinLibrary:
                        await scannerWorker.WriteAsync(
                            new SynchronizeJellyfinLibraries(library.MediaSourceId),
                            cancellationToken);
                        await scannerWorker.WriteAsync(
                            new ForceSynchronizeJellyfinLibraryById(library.Id, false),
                            cancellationToken);
                        break;
                    case EmbyLibrary:
                        await scannerWorker.WriteAsync(
                            new SynchronizeEmbyLibraries(library.MediaSourceId),
                            cancellationToken);
                        await scannerWorker.WriteAsync(
                            new ForceSynchronizeEmbyLibraryById(library.Id, false),
                            cancellationToken);
                        break;
                }
            }

            return true;
        }

        return false;
    }
}
