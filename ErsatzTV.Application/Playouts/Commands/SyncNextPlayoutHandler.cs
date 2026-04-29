using System.Collections.Immutable;
using System.Globalization;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Text;
using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlayoutItem = ErsatzTV.Core.Domain.PlayoutItem;

namespace ErsatzTV.Application.Playouts;

public partial class SyncNextPlayoutHandler(
    IFileSystem fileSystem,
    ILocalFileSystem localFileSystem,
    IPlexPathReplacementService plexPathReplacementService,
    IJellyfinPathReplacementService jellyfinPathReplacementService,
    IEmbyPathReplacementService embyPathReplacementService,
    ICustomStreamSelector customStreamSelector,
    IFFmpegStreamSelector ffmpegStreamSelector,
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
        // gen new folder name
        string versionFolderName = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

        string channelFolder = fileSystem.Path.Combine(FileSystemLayout.NextPlayoutsFolder, request.ChannelNumber);
        string versionFolder = fileSystem.Path.Combine(channelFolder, versionFolderName);

        logger.LogDebug("versioned playout folder is {Folder}", versionFolder);

        localFileSystem.EnsureFolderExists(versionFolder);

        await WriteAllJsonTo(request.ChannelNumber, versionFolder, cancellationToken);

        string currentFolder = fileSystem.Path.Combine(channelFolder, "current");

        // re-point symlink/junction to new folder
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (Directory.Exists(currentFolder))
            {
                var dirInfo = new DirectoryInfo(currentFolder);
                if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    dirInfo.Delete();
                }
                else
                {
                    logger.LogError("Expected junction at {Folder} but found a real directory", currentFolder);
                    return;
                }
            }

            var stdErrBuffer = new StringBuilder();
            CommandResult command = await Cli.Wrap("cmd.exe")
                .WithArguments(["/c", "mklink", "/j", "current", versionFolderName])
                .WithWorkingDirectory(channelFolder)
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(cancellationToken);

            if (!command.IsSuccess)
            {
                logger.LogError("Failed to link current playout JSON folder: {Error}", stdErrBuffer);
            }
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

        TimeSpan playoutOffset = TimeSpan.Zero;
        string mirrorChannelNumber = null;
        Option<Channel> maybeChannel = await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.MirrorSourceChannel)
            .Filter(c => c.PlayoutSource == ChannelPlayoutSource.Mirror && c.MirrorSourceChannelId != null)
            .SelectOneAsync(
                c => c.Number == channelNumber,
                c => c.Number == channelNumber,
                cancellationToken);
        foreach (Channel channel in maybeChannel)
        {
            mirrorChannelNumber = channel.MirrorSourceChannel.Number;
            playoutOffset = channel.PlayoutOffset ?? TimeSpan.Zero;
        }

        List<PlayoutItem> playoutItems = await dbContext.PlayoutItems
            .AsNoTracking()
            .Where(i => i.Playout.Channel.Number == (mirrorChannelNumber ?? channelNumber))
            .Include(i => i.MediaItem)
            .ThenInclude(mi => mi.LibraryPath)
            .ThenInclude(lp => lp.Library)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Episode).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Episode).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Episode).EpisodeMetadata)
            .ThenInclude(em => em.Subtitles)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Movie).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Movie).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Movie).MovieMetadata)
            .ThenInclude(mm => mm.Subtitles)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as OtherVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as OtherVideo).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as OtherVideo).OtherVideoMetadata)
            .ThenInclude(ovm => ovm.Subtitles)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .AsSplitQuery()
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

                MediaVersion headVersion = playoutItem.MediaItem.GetHeadVersion();

                playoutItem.Start += playoutOffset;
                playoutItem.Finish += playoutOffset;

                var nextPlayoutItem = new Core.Next.PlayoutItem
                {
                    Id = playoutItem.Id.ToString(CultureInfo.InvariantCulture),
                    Start = playoutItem.StartOffset,
                    Finish = playoutItem.FinishOffset
                };

                Option<Core.Next.Source> maybeSource = await SourceForItem(playoutItem, cancellationToken);
                if (maybeSource.IsNone)
                {
                    continue;
                }

                foreach (Core.Next.Source source in maybeSource)
                {
                    nextPlayoutItem.Source = source;
                }

                // if no audio streams, use lavfi to insert silence
                if (headVersion.Streams.All(s => s.MediaStreamKind is not MediaStreamKind.Audio))
                {
                    var videoSource = nextPlayoutItem.Source;

                    nextPlayoutItem.Source = null;
                    nextPlayoutItem.Tracks = new Core.Next.PlayoutItemTracks
                    {
                        Audio = new Core.Next.TrackSelection
                        {
                            Source =
                                new Core.Next.Source
                                {
                                    SourceType = Core.Next.SourceType.Lavfi,
                                    Params = "anullsrc=channel_layout=stereo:sample_rate=48000"
                                }
                        },
                        Video = new Core.Next.TrackSelection
                        {
                            Source = new Core.Next.Source
                            {
                                SourceType = videoSource.SourceType,
                                Path = videoSource.Path,
                            }
                        }
                    };
                }

                maybeChannel = await dbContext.Channels
                    .AsNoTracking()
                    .SingleOrDefaultAsync(c => c.Number == channelNumber, cancellationToken);
                foreach (Channel channel in maybeChannel)
                {
                    var audioVersion = new MediaItemAudioVersion(playoutItem.MediaItem, headVersion);
                    await SelectTracks(
                        channel,
                        audioVersion,
                        nextPlayoutItem,
                        playoutItem.PreferredAudioLanguageCode ?? channel.PreferredAudioLanguageCode,
                        playoutItem.PreferredAudioTitle ?? channel.PreferredAudioTitle,
                        playoutItem.PreferredSubtitleLanguageCode ?? channel.PreferredSubtitleLanguageCode,
                        playoutItem.SubtitleMode ?? channel.SubtitleMode,
                        cancellationToken);
                }

                playout.Items.Add(nextPlayoutItem);
            }

            await fileSystem.File.WriteAllTextAsync(fileName, Core.Next.Serialize.ToJson(playout), cancellationToken);
        }
    }

    private async Task SelectTracks(
        Channel channel,
        MediaItemAudioVersion audioVersion,
        Core.Next.PlayoutItem nextPlayoutItem,
        string preferredAudioLanguage,
        string preferredAudioTitle,
        string preferredSubtitleLanguage,
        ChannelSubtitleMode subtitleMode,
        CancellationToken cancellationToken)
    {
        List<Subtitle> allSubtitles = await GetSubtitles(audioVersion.MediaItem);

        Option<MediaStream> maybeAudioStream = Option<MediaStream>.None;
        Option<Subtitle> maybeSubtitle = Option<Subtitle>.None;

        if (channel.StreamSelectorMode is ChannelStreamSelectorMode.Custom)
        {
            StreamSelectorResult result = await customStreamSelector.SelectStreams(
                channel,
                nextPlayoutItem.Start,
                audioVersion,
                allSubtitles);
            maybeAudioStream = result.AudioStream;
            maybeSubtitle = result.Subtitle;
        }

        if (channel.StreamSelectorMode is ChannelStreamSelectorMode.Default || maybeAudioStream.IsNone)
        {
            maybeAudioStream =
                await ffmpegStreamSelector.SelectAudioStream(
                    audioVersion,
                    channel.StreamingMode,
                    channel,
                    preferredAudioLanguage,
                    preferredAudioTitle,
                    shouldLogMessages: false,
                    cancellationToken);

            maybeSubtitle =
                await ffmpegStreamSelector.SelectSubtitleStream(
                    allSubtitles.ToImmutableList(),
                    channel,
                    preferredSubtitleLanguage,
                    subtitleMode,
                    shouldLogMessages: false,
                    cancellationToken);
        }

        foreach (MediaStream audioStream in maybeAudioStream)
        {
            if (nextPlayoutItem.Tracks?.Audio?.StreamIndex is null)
            {
                nextPlayoutItem.Tracks ??= new Core.Next.PlayoutItemTracks();
                nextPlayoutItem.Tracks.Audio ??= new Core.Next.TrackSelection();
                nextPlayoutItem.Tracks.Audio.StreamIndex = audioStream.Index;
            }
        }

        foreach (Subtitle subtitle in maybeSubtitle)
        {
            if (subtitle.SubtitleKind is SubtitleKind.Embedded)
            {
                if (nextPlayoutItem.Tracks?.Subtitle?.StreamIndex is null)
                {
                    nextPlayoutItem.Tracks ??= new Core.Next.PlayoutItemTracks();
                    nextPlayoutItem.Tracks.Subtitle ??= new Core.Next.TrackSelection();
                    nextPlayoutItem.Tracks.Subtitle.StreamIndex = subtitle.StreamIndex;
                }
            }
            else if (!subtitle.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (nextPlayoutItem.Tracks?.Subtitle?.Source is null)
                {
                    nextPlayoutItem.Tracks ??= new Core.Next.PlayoutItemTracks();
                    nextPlayoutItem.Tracks.Subtitle ??= new Core.Next.TrackSelection();
                    nextPlayoutItem.Tracks.Subtitle.Source = new Core.Next.Source
                    {
                        SourceType = Core.Next.SourceType.Local,
                        Path = subtitle.Path,
                    };
                }
            }
        }
    }

    private async Task<Option<Core.Next.Source>> SourceForItem(
        PlayoutItem playoutItem,
        CancellationToken cancellationToken)
    {
        string path = await playoutItem.MediaItem.GetLocalPath(
            plexPathReplacementService,
            jellyfinPathReplacementService,
            embyPathReplacementService,
            cancellationToken);

        // check filesystem first
        if (fileSystem.File.Exists(path))
        {
            return new Core.Next.Source
            {
                SourceType = Core.Next.SourceType.Local,
                Path = path,
            };
        }

        MediaFile file = playoutItem.MediaItem.GetHeadVersion().MediaFiles.Head();
        int mediaSourceId = playoutItem.MediaItem.LibraryPath.Library.MediaSourceId;
        if (file is PlexMediaFile pmf)
        {
            return new Core.Next.Source
            {
                SourceType = Core.Next.SourceType.Http,
                Uri = $"http://localhost:{Settings.StreamingPort}/media/plex/{mediaSourceId}/{pmf.Key}"
            };
        }

        Option<string> jellyfinItemId = playoutItem.MediaItem switch
        {
            JellyfinEpisode e => e.ItemId,
            JellyfinMovie m => m.ItemId,
            _ => None
        };

        foreach (string itemId in jellyfinItemId)
        {
            return new Core.Next.Source
            {
                SourceType = Core.Next.SourceType.Http,
                Uri = $"http://localhost:{Settings.StreamingPort}/media/jellyfin/{itemId}"
            };
        }

        // attempt to remotely stream emby
        Option<string> embyItemId = playoutItem.MediaItem switch
        {
            EmbyEpisode e => e.ItemId,
            EmbyMovie m => m.ItemId,
            _ => None
        };

        foreach (string itemId in embyItemId)
        {
            return new Core.Next.Source
            {
                SourceType = Core.Next.SourceType.Http,
                Uri = $"http://localhost:{Settings.StreamingPort}/media/emby/{itemId}"
            };
        }

        return Option<Core.Next.Source>.None;
    }

    private void CleanOldVersions(
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

    private static async Task<List<Subtitle>> GetSubtitles(MediaItem mediaItem)
    {
        List<Subtitle> allSubtitles = mediaItem switch
        {
            Episode episode => await Optional(episode.EpisodeMetadata).Flatten().HeadOrNone()
                .Map(mm => mm.Subtitles ?? [])
                .IfNoneAsync([]),
            Movie movie => await Optional(movie.MovieMetadata).Flatten().HeadOrNone()
                .Map(mm => mm.Subtitles ?? [])
                .IfNoneAsync([]),
            //MusicVideo musicVideo => await GetMusicVideoSubtitles(musicVideo, channel, settings),
            OtherVideo otherVideo => await Optional(otherVideo.OtherVideoMetadata).Flatten().HeadOrNone()
                .Map(mm => mm.Subtitles ?? [])
                .IfNoneAsync([]),
            _ => []
        };

        bool isMediaServer = mediaItem is PlexMovie or PlexEpisode or
            JellyfinMovie or JellyfinEpisode or EmbyMovie or EmbyEpisode;

        if (isMediaServer)
        {
            return [];

            // closed captions are currently unsupported
            //allSubtitles.RemoveAll(s => s.Codec == "eia_608");
        }

        // TODO: external image subtitles
        allSubtitles.RemoveAll(s => s.IsImage && s.SubtitleKind is not SubtitleKind.Embedded);

        return allSubtitles;
    }
}
