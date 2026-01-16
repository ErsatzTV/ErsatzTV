using System.IO.Abstractions;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Maintenance;

public class DeleteOrphanedArtworkHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IArtworkRepository artworkRepository,
    IFileSystem fileSystem,
    ILogger<DeleteOrphanedArtworkHandler> logger)
    : IRequestHandler<DeleteOrphanedArtwork, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        DeleteOrphanedArtwork request,
        CancellationToken cancellationToken)
    {
        try
        {
            await CleanUpDatabase();
            await CleanUpFileSystem(cancellationToken);

            return Unit.Default;
        }
        catch (Exception e)
        {
            return BaseError.New(e.Message);
        }
    }

    private async Task CleanUpDatabase()
    {
        List<int> ids = await artworkRepository.GetOrphanedArtworkIds();
        if (ids.Count > 0)
        {
            await artworkRepository.Delete(ids);
        }
    }

    private async Task CleanUpFileSystem(CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        System.Collections.Generic.HashSet<string> validFiles = [];

        var lastId = 0;
        var hasMoreRows = true;
        while (hasMoreRows)
        {
            List<MinimalArtwork> result = await dbContext.Artwork
                .TagWithCallSite()
                .AsNoTracking()
                .Where(a => a.Id > lastId)
                .OrderBy(a => a.Id)
                .Take(1000)
                .Select(a => new MinimalArtwork(a.Id, a.Path, a.BlurHash43, a.BlurHash54, a.BlurHash64))
                .ToListAsync(cancellationToken);

            int newLastId = lastId;
            foreach (MinimalArtwork artwork in result)
            {
                newLastId = artwork.Id;

                if (!artwork.Path.Contains('/'))
                {
                    validFiles.Add(artwork.Path);
                }

                if (!string.IsNullOrWhiteSpace(artwork.BlurHash43))
                {
                    validFiles.Add(artwork.BlurHash43);
                }

                if (!string.IsNullOrWhiteSpace(artwork.BlurHash54))
                {
                    validFiles.Add(artwork.BlurHash54);
                }

                if (!string.IsNullOrWhiteSpace(artwork.BlurHash64))
                {
                    validFiles.Add(artwork.BlurHash64);
                }
            }

            if (lastId == newLastId)
            {
                hasMoreRows = false;
            }

            lastId = newLastId;
        }

        logger.LogDebug("Loaded {Count} artwork hashes (valid file names)", validFiles.Count);

        var deleted = 0;
        foreach (string file in fileSystem.Directory.EnumerateFiles(
                     FileSystemLayout.ArtworkCacheFolder,
                     "*.*",
                     SearchOption.AllDirectories))
        {
            string fileName = fileSystem.Path.GetFileName(file);
            if (!validFiles.Contains(fileName))
            {
                try
                {
                    fileSystem.File.Delete(file);
                    deleted++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not delete artwork file {File}", file);
                }
            }
        }

        logger.LogDebug("Deleted {Count} unused artwork cache files", deleted);

        DeleteEmptySubfolders(FileSystemLayout.ArtworkCacheFolder);
    }

    private void DeleteEmptySubfolders(string path)
    {
        if (!fileSystem.Directory.Exists(path))
        {
            return;
        }

        foreach (string sub in fileSystem.Directory.GetDirectories(path))
        {
            DeleteEmptySubfolders(sub);
        }

        if (!fileSystem.Directory.EnumerateFileSystemEntries(path).Any())
        {
            try
            {
                fileSystem.Directory.Delete(path);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete empty cache folder {Folder}", path);
            }
        }
    }

    private sealed record MinimalArtwork(int Id, string Path, string BlurHash43, string BlurHash54, string BlurHash64);
}
