using CliWrap;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Errors;
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

namespace ErsatzTV.Application.Troubleshooting;

public class PrepareTroubleshootingPlaybackHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IPlexPathReplacementService plexPathReplacementService,
    IJellyfinPathReplacementService jellyfinPathReplacementService,
    IEmbyPathReplacementService embyPathReplacementService,
    IFFmpegProcessService ffmpegProcessService,
    ILocalFileSystem localFileSystem,
    ILogger<PrepareTroubleshootingPlaybackHandler> logger)
    : IRequestHandler<PrepareTroubleshootingPlayback, Either<BaseError, Command>>
{
    public async Task<Either<BaseError, Command>> Handle(PrepareTroubleshootingPlayback request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Tuple<MediaItem, string, string, FFmpegProfile>> validation = await Validate(dbContext, request);
        return await validation.Match(
            tuple => GetProcess(dbContext, tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4),
            error => Task.FromResult<Either<BaseError, Command>>(error.Join()));
    }

    private async Task<Either<BaseError, Command>> GetProcess(
        TvContext dbContext,
        MediaItem mediaItem,
        string ffmpegPath,
        string ffprobePath,
        FFmpegProfile ffmpegProfile)
    {
        string transcodeFolder = Path.Combine(FileSystemLayout.TranscodeFolder, ".troubleshooting");

        localFileSystem.EnsureFolderExists(transcodeFolder);
        localFileSystem.EmptyFolder(transcodeFolder);

        ChannelSubtitleMode subtitleMode = ChannelSubtitleMode.None;

        MediaVersion version = mediaItem.GetHeadVersion();

        string mediaPath = await GetMediaItemPath(dbContext, mediaItem);
        if (string.IsNullOrEmpty(mediaPath))
        {
            logger.LogWarning("Media item {MediaItemId} does not exist on disk; cannot troubleshoot.", mediaItem.Id);
            return BaseError.New("Media item does not exist on disk");
        }

        DateTimeOffset now = DateTimeOffset.Now;

        var duration = TimeSpan.FromSeconds(Math.Min(version.Duration.TotalSeconds, 30));

        Command process = await ffmpegProcessService.ForPlayoutItem(
            ffmpegPath,
            ffprobePath,
            true,
            new Channel(Guid.Empty)
            {
                Number = ".troubleshooting",
                FFmpegProfile = ffmpegProfile,
                StreamingMode = StreamingMode.HttpLiveStreamingSegmenter,
                SubtitleMode = subtitleMode
            },
            version,
            new MediaItemAudioVersion(mediaItem, version),
            mediaPath,
            mediaPath,
            _ => Task.FromResult(new List<Subtitle>()),
            string.Empty,
            string.Empty,
            string.Empty,
            subtitleMode,
            now,
            now + duration,
            now,
            Option<ChannelWatermark>.None,
            Option<ChannelWatermark>.None,
            ffmpegProfile.VaapiDisplay,
            ffmpegProfile.VaapiDriver,
            ffmpegProfile.VaapiDevice,
            Option<int>.None,
            false,
            FillerKind.None,
            TimeSpan.Zero,
            duration,
            0,
            None,
            false,
            transcodeFolder,
            _ => { });

        return process;
    }

    private static async Task<Validation<BaseError, Tuple<MediaItem, string, string, FFmpegProfile>>> Validate(
        TvContext dbContext,
        PrepareTroubleshootingPlayback request) =>
        (await MediaItemMustExist(dbContext, request),
            await FFmpegPathMustExist(dbContext),
            await FFprobePathMustExist(dbContext),
            await FFmpegProfileMustExist(dbContext, request))
        .Apply((mediaItem, ffmpegPath, ffprobePath, ffmpegProfile) =>
            Tuple(mediaItem, ffmpegPath, ffprobePath, ffmpegProfile));

    private static async Task<Validation<BaseError, MediaItem>> MediaItemMustExist(
        TvContext dbContext,
        PrepareTroubleshootingPlayback request)
    {
        return await dbContext.MediaItems
            .Include(mi => (mi as Episode).EpisodeMetadata)
            .ThenInclude(em => em.Subtitles)
            .Include(mi => (mi as Episode).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as Episode).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(mi => (mi as Episode).Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .Include(mi => (mi as Movie).MovieMetadata)
            .ThenInclude(mm => mm.Subtitles)
            .Include(mi => (mi as Movie).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as Movie).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(mi => (mi as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Subtitles)
            .Include(mi => (mi as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Artists)
            .Include(mi => (mi as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Studios)
            .Include(mi => (mi as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Directors)
            .Include(mi => (mi as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(mi => (mi as MusicVideo).Artist)
            .ThenInclude(mv => mv.ArtistMetadata)
            .Include(mi => (mi as OtherVideo).OtherVideoMetadata)
            .ThenInclude(ovm => ovm.Subtitles)
            .Include(mi => (mi as OtherVideo).MediaVersions)
            .ThenInclude(ov => ov.MediaFiles)
            .Include(mi => (mi as OtherVideo).MediaVersions)
            .ThenInclude(ov => ov.Streams)
            .Include(mi => (mi as Song).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as Song).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(mi => (mi as Song).SongMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(mi => (mi as Image).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as Image).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(mi => (mi as Image).ImageMetadata)
            .SelectOneAsync(mi => mi.Id, mi => mi.Id == request.MediaItemId)
            .Map(o => o.ToValidation<BaseError>(new UnableToLocatePlayoutItem()));
    }

    private static Task<Validation<BaseError, string>> FFmpegPathMustExist(TvContext dbContext) =>
        dbContext.ConfigElements.GetValue<string>(ConfigElementKey.FFmpegPath)
            .FilterT(File.Exists)
            .Map(maybePath => maybePath.ToValidation<BaseError>("FFmpeg path does not exist on filesystem"));

    private static Task<Validation<BaseError, string>> FFprobePathMustExist(TvContext dbContext) =>
        dbContext.ConfigElements.GetValue<string>(ConfigElementKey.FFprobePath)
            .FilterT(File.Exists)
            .Map(maybePath => maybePath.ToValidation<BaseError>("FFprobe path does not exist on filesystem"));

    private static Task<Validation<BaseError, FFmpegProfile>> FFmpegProfileMustExist(
        TvContext dbContext,
        PrepareTroubleshootingPlayback request) =>
        dbContext.FFmpegProfiles
            .Include(p => p.Resolution)
            .SelectOneAsync(p => p.Id, p => p.Id == request.FFmpegProfileId)
            .Map(o => o.ToValidation<BaseError>($"FFmpegProfile {request.FFmpegProfileId} does not exist"));

    private async Task<string> GetMediaItemPath(
        TvContext dbContext,
        MediaItem mediaItem)
    {
        string path = await GetLocalPath(mediaItem);

        // check filesystem first
        if (localFileSystem.FileExists(path))
        {
            return path;
        }

        // attempt to remotely stream plex
        MediaFile file = mediaItem.GetHeadVersion().MediaFiles.Head();
        switch (file)
        {
            case PlexMediaFile pmf:
                Option<int> maybeId = await dbContext.Connection.QuerySingleOrDefaultAsync<int>(
                        @"SELECT PMS.Id FROM PlexMediaSource PMS
                  INNER JOIN Library L on PMS.Id = L.MediaSourceId
                  INNER JOIN LibraryPath LP on L.Id = LP.LibraryId
                  WHERE LP.Id = @LibraryPathId",
                        new { mediaItem.LibraryPathId })
                    .Map(Optional);

                foreach (int plexMediaSourceId in maybeId)
                {
                    logger.LogDebug(
                        "Attempting to stream Plex file {PlexFileName} using key {PlexKey}",
                        pmf.Path,
                        pmf.Key);

                    return $"http://localhost:{Settings.StreamingPort}/media/plex/{plexMediaSourceId}/{pmf.Key}";
                }

                break;
        }

        // attempt to remotely stream jellyfin
        Option<string> jellyfinItemId = mediaItem switch
        {
            JellyfinEpisode e => e.ItemId,
            JellyfinMovie m => m.ItemId,
            _ => None
        };

        foreach (string itemId in jellyfinItemId)
        {
            return $"http://localhost:{Settings.StreamingPort}/media/jellyfin/{itemId}";
        }

        // attempt to remotely stream emby
        Option<string> embyItemId = mediaItem switch
        {
            EmbyEpisode e => e.ItemId,
            EmbyMovie m => m.ItemId,
            _ => None
        };

        foreach (string itemId in embyItemId)
        {
            return $"http://localhost:{Settings.StreamingPort}/media/emby/{itemId}";
        }

        return null;
    }

    private async Task<string> GetLocalPath(MediaItem mediaItem)
    {
        MediaVersion version = mediaItem.GetHeadVersion();
        MediaFile file = version.MediaFiles.Head();

        string path = file.Path;
        return mediaItem switch
        {
            PlexMovie plexMovie => await plexPathReplacementService.GetReplacementPlexPath(
                plexMovie.LibraryPathId,
                path),
            PlexEpisode plexEpisode => await plexPathReplacementService.GetReplacementPlexPath(
                plexEpisode.LibraryPathId,
                path),
            JellyfinMovie jellyfinMovie => await jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinMovie.LibraryPathId,
                path),
            JellyfinEpisode jellyfinEpisode => await jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinEpisode.LibraryPathId,
                path),
            EmbyMovie embyMovie => await embyPathReplacementService.GetReplacementEmbyPath(
                embyMovie.LibraryPathId,
                path),
            EmbyEpisode embyEpisode => await embyPathReplacementService.GetReplacementEmbyPath(
                embyEpisode.LibraryPathId,
                path),
            _ => path
        };
    }
}
