using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Images;

public class UpdateImageFolderDurationHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<UpdateImageFolderDuration, int?>
{
    public async Task<int?> Handle(UpdateImageFolderDuration request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (request.ImageFolderDuration.IfNone(1) < 1)
        {
            request = request with { ImageFolderDuration = 1 };
        }
        
        // delete entry if null
        if (request.ImageFolderDuration is null)
        {
            await dbContext.ImageFolderDurations
                .Filter(ifd => ifd.LibraryFolderId == request.LibraryFolderId)
                .ExecuteDeleteAsync(cancellationToken);
        }
        // upsert if non-null
        else
        {
            Option<ImageFolderDuration> maybeExisting = await dbContext.ImageFolderDurations
                .SelectOneAsync(ifd => ifd.LibraryFolderId, ifd => ifd.LibraryFolderId == request.LibraryFolderId);

            if (maybeExisting.IsNone)
            {
                var entry = new ImageFolderDuration
                {
                    LibraryFolderId = request.LibraryFolderId
                };

                maybeExisting = entry;

                await dbContext.ImageFolderDurations.AddAsync(entry, cancellationToken);
            }

            foreach (ImageFolderDuration existing in maybeExisting)
            {
                existing.DurationSeconds = request.ImageFolderDuration.Value;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        
        // update all images (bfs) starting at this folder
        Option<LibraryFolder> maybeFolder = await dbContext.LibraryFolders
            .AsNoTracking()
            .Include(lf => lf.ImageFolderDuration)
            .SelectOneAsync(lf => lf.Id, lf => lf.Id == request.LibraryFolderId);

        var queue = new Queue<FolderWithParentDuration>();
        foreach (LibraryFolder libraryFolder in maybeFolder)
        {
            LibraryFolder currentFolder = libraryFolder;
            
            // walk up to get duration, if needed
            int? durationSeconds = currentFolder.ImageFolderDuration?.DurationSeconds;
            while (durationSeconds is null && currentFolder?.ParentId is not null)
            {
                Option<LibraryFolder> maybeParent = await dbContext.LibraryFolders
                    .AsNoTracking()
                    .Include(lf => lf.ImageFolderDuration)
                    .SelectOneAsync(lf => lf.Id, lf => lf.Id == currentFolder.ParentId);

                if (maybeParent.IsNone)
                {
                    currentFolder = null;
                }
                
                foreach (LibraryFolder parent in maybeParent)
                {
                    currentFolder = parent;
                    durationSeconds = currentFolder.ImageFolderDuration?.DurationSeconds;
                }
            }

            queue.Enqueue(new FolderWithParentDuration(libraryFolder, durationSeconds));
        }
        
        while (queue.Count > 0)
        {
            (LibraryFolder currentFolder, int? parentDuration) = queue.Dequeue();
            int? effectiveDuration = currentFolder.ImageFolderDuration?.DurationSeconds ?? parentDuration;

            // Serilog.Log.Logger.Information(
            //     "Updating folder {Id} with parent duration {ParentDuration}, effective duration {EffectiveDuration}",
            //     currentFolder.Id,
            //     parentDuration,
            //     effectiveDuration);

            // update all images in this folder
            await dbContext.ImageMetadata
                .Filter(
                    im => im.Image.MediaVersions.Any(
                        mv => mv.MediaFiles.Any(mf => mf.LibraryFolderId == currentFolder.Id)))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(im => im.DurationSeconds, effectiveDuration),
                    cancellationToken);

            List<LibraryFolder> children = await dbContext.LibraryFolders
                .AsNoTracking()
                .Filter(lf => lf.ParentId == currentFolder.Id)
                .Include(lf => lf.ImageFolderDuration)
                .ToListAsync(cancellationToken);
            
            // queue all children
            foreach (LibraryFolder child in children)
            {
                queue.Enqueue(new FolderWithParentDuration(child, effectiveDuration));
            }
        }

        return request.ImageFolderDuration;
    }

    private sealed record FolderWithParentDuration(LibraryFolder LibraryFolder, int? ParentDuration);
}
