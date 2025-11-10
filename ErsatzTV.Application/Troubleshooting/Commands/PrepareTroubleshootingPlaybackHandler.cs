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
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Notifications;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.State;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Events;

namespace ErsatzTV.Application.Troubleshooting;

public class PrepareTroubleshootingPlaybackHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IPlexPathReplacementService plexPathReplacementService,
    IJellyfinPathReplacementService jellyfinPathReplacementService,
    IEmbyPathReplacementService embyPathReplacementService,
    IFFmpegProcessService ffmpegProcessService,
    ILocalFileSystem localFileSystem,
    ISongVideoGenerator songVideoGenerator,
    IWatermarkSelector watermarkSelector,
    IEntityLocker entityLocker,
    IMediator mediator,
    LoggingLevelSwitches loggingLevelSwitches,
    ILogger<PrepareTroubleshootingPlaybackHandler> logger)
    : IRequestHandler<PrepareTroubleshootingPlayback, Either<BaseError, PlayoutItemResult>>
{
    public async Task<Either<BaseError, PlayoutItemResult>> Handle(
        PrepareTroubleshootingPlayback request,
        CancellationToken cancellationToken)
    {
        var currentStreamingLevel = loggingLevelSwitches.StreamingLevelSwitch.MinimumLevel;
        loggingLevelSwitches.StreamingLevelSwitch.MinimumLevel = LogEventLevel.Debug;

        try
        {
            using var logContext = LogContext.PushProperty(InMemoryLogService.CorrelationIdKey, request.SessionId);
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            Validation<BaseError, Tuple<MediaItem, string, string, FFmpegProfile>> validation = await Validate(
                dbContext,
                request,
                cancellationToken);
            return await validation.Match(
                tuple => GetProcess(
                    dbContext,
                    request,
                    tuple.Item1,
                    tuple.Item2,
                    tuple.Item3,
                    tuple.Item4,
                    cancellationToken),
                error => Task.FromResult<Either<BaseError, PlayoutItemResult>>(error.Join()));
        }
        catch (Exception ex)
        {
            entityLocker.UnlockTroubleshootingPlayback();
            await mediator.Publish(new PlaybackTroubleshootingCompletedNotification(-1, ex, Option<double>.None), cancellationToken);
            logger.LogError(ex, "Error while preparing troubleshooting playback");
            return BaseError.New(ex.Message);
        }
        finally
        {
            loggingLevelSwitches.StreamingLevelSwitch.MinimumLevel = currentStreamingLevel;
        }
    }

    private async Task<Either<BaseError, PlayoutItemResult>> GetProcess(
        TvContext dbContext,
        PrepareTroubleshootingPlayback request,
        MediaItem mediaItem,
        string ffmpegPath,
        string ffprobePath,
        FFmpegProfile ffmpegProfile,
        CancellationToken cancellationToken)
    {
        if (entityLocker.IsTroubleshootingPlaybackLocked())
        {
            return BaseError.New("Troubleshooting playback is locked");
        }

        entityLocker.LockTroubleshootingPlayback();

        localFileSystem.EnsureFolderExists(FileSystemLayout.TranscodeTroubleshootingFolder);
        localFileSystem.EmptyFolder(FileSystemLayout.TranscodeTroubleshootingFolder);

        const ChannelSubtitleMode SUBTITLE_MODE = ChannelSubtitleMode.Any;

        MediaVersion version = mediaItem.GetHeadVersion();

        string mediaPath = await GetMediaItemPath(dbContext, mediaItem, cancellationToken);
        if (string.IsNullOrEmpty(mediaPath))
        {
            logger.LogWarning("Media item {MediaItemId} does not exist on disk; cannot troubleshoot.", mediaItem.Id);
            return BaseError.New("Media item does not exist on disk");
        }

        var channel = new Channel(Guid.Empty)
        {
            Artwork = [],
            Name = "ETV",
            Number = ".troubleshooting",
            FFmpegProfile = ffmpegProfile,
            StreamingMode = request.StreamingMode,
            StreamSelectorMode = ChannelStreamSelectorMode.Troubleshooting,
            SubtitleMode = SUBTITLE_MODE
            //SongVideoMode = ChannelSongVideoMode.WithProgress
        };

        if (!string.IsNullOrEmpty(request.StreamSelector))
        {
            channel.StreamSelectorMode = ChannelStreamSelectorMode.Custom;
            channel.StreamSelector = request.StreamSelector;
        }

        List<WatermarkOptions> watermarks = [];
        if (request.WatermarkIds.Count > 0)
        {
            List<ChannelWatermark> channelWatermarks = await dbContext.ChannelWatermarks
                .AsNoTracking()
                .Where(w => request.WatermarkIds.Contains(w.Id))
                .ToListAsync(cancellationToken);

            foreach (var watermark in channelWatermarks)
            {
                watermarks.AddRange(
                    watermarkSelector.GetWatermarkOptions(channel, watermark, Option<ChannelWatermark>.None));
            }
        }

        string videoPath = mediaPath;
        MediaVersion videoVersion = version;

        if (mediaItem is Song song)
        {
            (videoPath, videoVersion) = await songVideoGenerator.GenerateSongVideo(
                song,
                channel,
                ffmpegPath,
                ffprobePath,
                CancellationToken.None);

            // override watermark as song_progress_overlay.png
            if (videoVersion is BackgroundImageMediaVersion { IsSongWithProgress: true })
            {
                double ratio = channel.FFmpegProfile.Resolution.Width /
                               (double)channel.FFmpegProfile.Resolution.Height;
                bool is43 = Math.Abs(ratio - 4.0 / 3.0) < 0.01;
                string image = is43 ? "song_progress_overlay_43.png" : "song_progress_overlay.png";

                var progressWatermark = new ChannelWatermark
                {
                    Mode = ChannelWatermarkMode.Permanent,
                    Size = WatermarkSize.Scaled,
                    WidthPercent = 100,
                    HorizontalMarginPercent = 0,
                    VerticalMarginPercent = 0,
                    Opacity = 100,
                    Location = WatermarkLocation.TopLeft,
                    ImageSource = ChannelWatermarkImageSource.Resource,
                    Image = image
                };

                var progressWatermarkOption = new WatermarkOptions(
                    progressWatermark,
                    Path.Combine(FileSystemLayout.ResourcesCacheFolder, progressWatermark.Image),
                    Option<int>.None);

                watermarks.Clear();
                watermarks.Add(progressWatermarkOption);
            }
        }

        DateTimeOffset now = DateTimeOffset.Now;

        var duration = TimeSpan.FromSeconds(Math.Min(version.Duration.TotalSeconds, 30));
        if (duration <= TimeSpan.Zero)
        {
            duration = TimeSpan.FromSeconds(30);
        }

        // we cannot burst live input
        bool hlsRealtime = mediaItem is RemoteStream { IsLive: true };

        TimeSpan inPoint = TimeSpan.Zero;
        TimeSpan outPoint = duration;
        if (!hlsRealtime)
        {
            foreach (int seekSeconds in request.SeekSeconds)
            {
                inPoint = TimeSpan.FromSeconds(seekSeconds);
                if (inPoint > version.Duration)
                {
                    inPoint = version.Duration - duration;
                }

                if (inPoint + duration > version.Duration)
                {
                    duration = version.Duration - inPoint;
                }

                outPoint = inPoint + duration;
            }
        }

        List<GraphicsElement> graphicsElements = await dbContext.GraphicsElements
            .Where(ge => request.GraphicsElementIds.Contains(ge.Id))
            .ToListAsync(cancellationToken);

        PlayoutItemResult playoutItemResult = await ffmpegProcessService.ForPlayoutItem(
            ffmpegPath,
            ffprobePath,
            true,
            channel,
            new MediaItemVideoVersion(mediaItem, videoVersion),
            new MediaItemAudioVersion(mediaItem, version),
            videoPath,
            mediaPath,
            _ => GetSubtitles(mediaItem, request),
            string.Empty,
            string.Empty,
            string.Empty,
            SUBTITLE_MODE,
            now,
            now + duration,
            now,
            watermarks,
            graphicsElements.Map(ge => new PlayoutItemGraphicsElement { GraphicsElement = ge }).ToList(),
            ffmpegProfile.VaapiDisplay,
            ffmpegProfile.VaapiDriver,
            ffmpegProfile.VaapiDevice,
            Option<int>.None,
            hlsRealtime,
            mediaItem is RemoteStream { IsLive: true } ? StreamInputKind.Live : StreamInputKind.Vod,
            FillerKind.None,
            inPoint,
            channelStartTime: DateTimeOffset.Now,
            TimeSpan.Zero,
            Option<int>.None,
            FileSystemLayout.TranscodeTroubleshootingFolder,
            _ => { },
            canProxy: true,
            cancellationToken);

        return playoutItemResult;
    }

    private static async Task<List<Subtitle>> GetSubtitles(MediaItem mediaItem, PrepareTroubleshootingPlayback request)
    {
        List<Subtitle> allSubtitles = mediaItem switch
        {
            Episode episode => await Optional(episode.EpisodeMetadata).Flatten().HeadOrNone()
                .Map(mm => mm.Subtitles ?? [])
                .IfNoneAsync([]),
            Movie movie => await Optional(movie.MovieMetadata).Flatten().HeadOrNone()
                .Map(mm => mm.Subtitles ?? [])
                .IfNoneAsync([]),
            OtherVideo otherVideo => await Optional(otherVideo.OtherVideoMetadata).Flatten().HeadOrNone()
                .Map(mm => mm.Subtitles ?? [])
                .IfNoneAsync([]),
            _ => []
        };

        bool isMediaServer = mediaItem is PlexMovie or PlexEpisode or
            JellyfinMovie or JellyfinEpisode or EmbyMovie or EmbyEpisode;

        if (isMediaServer)
        {
            // closed captions are currently unsupported
            allSubtitles.RemoveAll(s => s.Codec == "eia_608");
        }

        if (request.SubtitleId is not null)
        {
            allSubtitles.RemoveAll(s => s.Id != request.SubtitleId.Value);

            foreach (Subtitle subtitle in allSubtitles)
            {
                // pretend subtitle is forced
                subtitle.Forced = true;
                return [subtitle];
            }
        }
        else if (string.IsNullOrWhiteSpace(request.StreamSelector))
        {
            allSubtitles.Clear();
        }

        return allSubtitles;
    }

    private static async Task<Validation<BaseError, Tuple<MediaItem, string, string, FFmpegProfile>>> Validate(
        TvContext dbContext,
        PrepareTroubleshootingPlayback request,
        CancellationToken cancellationToken) =>
        (await MediaItemMustExist(dbContext, request, cancellationToken),
            await FFmpegPathMustExist(dbContext, cancellationToken),
            await FFprobePathMustExist(dbContext, cancellationToken),
            await FFmpegProfileMustExist(dbContext, request, cancellationToken))
        .Apply((mediaItem, ffmpegPath, ffprobePath, ffmpegProfile) =>
            Tuple(mediaItem, ffmpegPath, ffprobePath, ffmpegProfile));

    private static async Task<Validation<BaseError, MediaItem>> MediaItemMustExist(
        TvContext dbContext,
        PrepareTroubleshootingPlayback request,
        CancellationToken cancellationToken) =>
        await dbContext.MediaItems
            .AsNoTracking()
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
            .Include(mi => (mi as RemoteStream).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as RemoteStream).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(mi => (mi as RemoteStream).RemoteStreamMetadata)
            .SelectOneAsync(mi => mi.Id, mi => mi.Id == request.MediaItemId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>(new UnableToLocatePlayoutItem()));

    private static Task<Validation<BaseError, string>> FFmpegPathMustExist(
        TvContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.ConfigElements.GetValue<string>(ConfigElementKey.FFmpegPath, cancellationToken)
            .FilterT(File.Exists)
            .Map(maybePath => maybePath.ToValidation<BaseError>("FFmpeg path does not exist on filesystem"));

    private static Task<Validation<BaseError, string>> FFprobePathMustExist(
        TvContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.ConfigElements.GetValue<string>(ConfigElementKey.FFprobePath, cancellationToken)
            .FilterT(File.Exists)
            .Map(maybePath => maybePath.ToValidation<BaseError>("FFprobe path does not exist on filesystem"));

    private static Task<Validation<BaseError, FFmpegProfile>> FFmpegProfileMustExist(
        TvContext dbContext,
        PrepareTroubleshootingPlayback request,
        CancellationToken cancellationToken) =>
        dbContext.FFmpegProfiles
            .Include(p => p.Resolution)
            .SelectOneAsync(p => p.Id, p => p.Id == request.FFmpegProfileId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>($"FFmpegProfile {request.FFmpegProfileId} does not exist"));

    private async Task<string> GetMediaItemPath(
        TvContext dbContext,
        MediaItem mediaItem,
        CancellationToken cancellationToken)
    {
        string path = await GetLocalPath(mediaItem, cancellationToken);

        // check filesystem first
        if (localFileSystem.FileExists(path))
        {
            if (mediaItem is RemoteStream remoteStream)
            {
                path = !string.IsNullOrWhiteSpace(remoteStream.Url)
                    ? remoteStream.Url
                    : $"http://localhost:{Settings.StreamingPort}/ffmpeg/remote-stream/{remoteStream.Id}";
            }

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

    private async Task<string> GetLocalPath(MediaItem mediaItem, CancellationToken cancellationToken)
    {
        MediaVersion version = mediaItem.GetHeadVersion();
        MediaFile file = version.MediaFiles.Head();

        string path = file.Path;
        return mediaItem switch
        {
            PlexMovie plexMovie => await plexPathReplacementService.GetReplacementPlexPath(
                plexMovie.LibraryPathId,
                path,
                cancellationToken),
            PlexEpisode plexEpisode => await plexPathReplacementService.GetReplacementPlexPath(
                plexEpisode.LibraryPathId,
                path,
                cancellationToken),
            JellyfinMovie jellyfinMovie => await jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinMovie.LibraryPathId,
                path,
                cancellationToken),
            JellyfinEpisode jellyfinEpisode => await jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinEpisode.LibraryPathId,
                path,
                cancellationToken),
            EmbyMovie embyMovie => await embyPathReplacementService.GetReplacementEmbyPath(
                embyMovie.LibraryPathId,
                path,
                cancellationToken),
            EmbyEpisode embyEpisode => await embyPathReplacementService.GetReplacementEmbyPath(
                embyEpisode.LibraryPathId,
                path,
                cancellationToken),
            _ => path
        };
    }
}
