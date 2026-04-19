using System.Globalization;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Next;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Playouts;

public partial class SyncNextPlayoutHandler(
    IFileSystem fileSystem,
    ILocalFileSystem localFileSystem,
    IDbContextFactory<TvContext> dbContextFactory,
    ILogger<SyncNextPlayoutHandler> logger)
    : IRequestHandler<SyncNextPlayout>
{
    [LibraryImport("libc", EntryPoint = "rename", SetLastError = true)]
    private static partial int Rename(
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        string oldpath,
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        string newpath
    );

    public async Task Handle(SyncNextPlayout request, CancellationToken cancellationToken)
    {
        // TODO: NEXT: support junctions on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // gen new folder name
        string versionFolderName = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

        string versionFolder = fileSystem.Path.Combine(
            FileSystemLayout.NextPlayoutsFolder,
            request.ChannelNumber,
            versionFolderName);

        logger.LogDebug("versioned playout folder is {Folder}", versionFolder);

        localFileSystem.EnsureFolderExists(versionFolder);

        await WriteAllJsonTo(request.ChannelNumber, versionFolder, cancellationToken);

        string currentFolder = fileSystem.Path.Combine(
            FileSystemLayout.NextPlayoutsFolder,
            request.ChannelNumber,
            "current");

        // re-point symlink/junction to new folder
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
        }
        else
        {
            string tempLink = fileSystem.Path.Combine(
                FileSystemLayout.NextPlayoutsFolder,
                request.ChannelNumber,
                fileSystem.Path.GetRandomFileName());

            fileSystem.File.CreateSymbolicLink(tempLink, versionFolderName);
            _ = Rename(tempLink, currentFolder);
        }

        CleanOldVersions(
            fileSystem.Path.Combine(FileSystemLayout.NextPlayoutsFolder, request.ChannelNumber),
            currentFolder);
    }

    private async Task WriteAllJsonTo(string channelNumber, string targetFolder, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<int> localLibraryIds = await dbContext.LocalLibraries
            .AsNoTracking()
            .Map(l => l.Id)
            .ToListAsync(cancellationToken);

        List<PlayoutItem> playoutItems = await dbContext.PlayoutItems
            .AsNoTracking()
            .Where(i => i.Playout.Channel.Number == channelNumber)
            .Where(i => localLibraryIds.Contains(i.MediaItem.LibraryPath.LibraryId))
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Episode).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Movie).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as OtherVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .ToListAsync(cancellationToken);

        logger.LogDebug("Located {Count} local playout items", playoutItems.Count);

        foreach (IGrouping<DateTime, PlayoutItem> group in playoutItems.GroupBy(pi => pi.StartOffset.Date)
                     .Where(g => g.Any()))
        {
            var first = group.First();
            var last = group.Last();

            string fileName = fileSystem.Path.Combine(
                targetFolder,
                $"{first.StartOffset.ToUnixTimeMilliseconds()}_{last.FinishOffset.ToUnixTimeMilliseconds()}.json");

            var playout = new Core.Next.Playout { Version = "https://ersatztv.org/playout/version/0.0.1", Items = [] };
            foreach (PlayoutItem playoutItem in group)
            {
                if (playoutItem.MediaItem is not Episode && playoutItem.MediaItem is not Movie &&
                    playoutItem.MediaItem is not OtherVideo && playoutItem.MediaItem is not MusicVideo)
                {
                    continue;
                }

                string path = playoutItem.MediaItem.GetHeadVersion().MediaFiles.Head().Path;

                var nextPlayoutItem = new ItemElement
                {
                    Id = playoutItem.Id.ToString(CultureInfo.InvariantCulture),
                    Start = playoutItem.StartOffset.ToString("O"),
                    Finish = playoutItem.FinishOffset.ToString("O"),
                    Source = new ItemSource
                    {
                        SourceType = SourceType.Local,
                        Path = path,
                    }
                };

                playout.Items.Add(nextPlayoutItem);
            }

            await fileSystem.File.WriteAllTextAsync(fileName, playout.ToJson(), cancellationToken);
        }
    }

    public void CleanOldVersions(
        string playoutRoot,
        string currentLinkPath,
        int keepVersions = 2,
        TimeSpan? gracePeriod = null)
    {
        gracePeriod ??= TimeSpan.FromMinutes(5);

        string currentResolvedPath = null;
        if (Directory.Exists(currentLinkPath))
        {
            currentResolvedPath = Path.GetFullPath(
                Path.Combine(
                    Path.GetDirectoryName(currentLinkPath) ?? "",
                    Directory.ResolveLinkTarget(currentLinkPath, true)?.FullName ?? ""
                ));
        }

        var directories = Directory.GetDirectories(playoutRoot)
            .Select(d => new DirectoryInfo(d))
            .Where(d => long.TryParse(d.Name, out _))
            .OrderByDescending(d => d.Name)
            .ToList();

        int keptCount = 0;

        foreach (var dir in directories)
        {
            string fullDir = dir.FullName;

            if (fullDir.Equals(currentResolvedPath, StringComparison.OrdinalIgnoreCase))
            {
                keptCount++;
                continue;
            }

            if (keptCount < keepVersions)
            {
                keptCount++;
                continue;
            }

            if (DateTime.Now - dir.LastWriteTime < gracePeriod)
            {
                continue;
            }

            try
            {
                dir.Delete(recursive: true);
                logger.LogDebug("Cleaned up old playout version: {Folder}", dir.Name);
            }
            catch (IOException)
            {
                // ignore errors; will be cleaned up next time through
                logger.LogDebug("Skipping busy folder: {Folder}", dir.Name);
            }
        }
    }
}
