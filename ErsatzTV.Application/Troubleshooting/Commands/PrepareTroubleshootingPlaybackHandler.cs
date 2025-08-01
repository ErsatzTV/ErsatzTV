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
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.FFmpeg;
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
    IEntityLocker entityLocker,
    ILogger<PrepareTroubleshootingPlaybackHandler> logger)
    : IRequestHandler<PrepareTroubleshootingPlayback, Either<BaseError, Command>>
{
    public async Task<Either<BaseError, Command>> Handle(PrepareTroubleshootingPlayback request, CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            Validation<BaseError, Tuple<MediaItem, string, string, FFmpegProfile>> validation = await Validate(dbContext, request);
            return await validation.Match(
                tuple => GetProcess(dbContext, request, tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4),
                error => Task.FromResult<Either<BaseError, Command>>(error.Join()));
        }
        catch (Exception ex)
        {
            entityLocker.UnlockTroubleshootingPlayback();
            logger.LogError(ex, "Error while preparing troubleshooting playback");
            return BaseError.New(ex.Message);
        }
    }

    private async Task<Either<BaseError, Command>> GetProcess(
        TvContext dbContext,
        PrepareTroubleshootingPlayback request,
        MediaItem mediaItem,
        string ffmpegPath,
        string ffprobePath,
        FFmpegProfile ffmpegProfile)
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

        string mediaPath = await GetMediaItemPath(dbContext, mediaItem);
        if (string.IsNullOrEmpty(mediaPath))
        {
            logger.LogWarning("Media item {MediaItemId} does not exist on disk; cannot troubleshoot.", mediaItem.Id);
            return BaseError.New("Media item does not exist on disk");
        }

        Option<ChannelWatermark> maybeWatermark = Option<ChannelWatermark>.None;
        if (request.WatermarkId > 0)
        {
            maybeWatermark = await dbContext.ChannelWatermarks
                .SelectOneAsync(cw => cw.Id, cw => cw.Id == request.WatermarkId);
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
        if (!hlsRealtime && !request.StartFromBeginning)
        {
            inPoint = TimeSpan.FromSeconds(version.Duration.TotalSeconds / 2.0);
            if (inPoint.TotalSeconds < 30)
            {
                duration = inPoint;
            }
            outPoint = inPoint + duration;
        }

        Command process = await ffmpegProcessService.ForPlayoutItem(
            ffmpegPath,
            ffprobePath,
            true,
            new Channel(Guid.Empty)
            {
                Number = ".troubleshooting",
                FFmpegProfile = ffmpegProfile,
                StreamingMode = StreamingMode.HttpLiveStreamingSegmenter,
                StreamSelectorMode = ChannelStreamSelectorMode.Troubleshooting,
                SubtitleMode = SUBTITLE_MODE
            },
            version,
            new MediaItemAudioVersion(mediaItem, version),
            mediaPath,
            mediaPath,
            _ => GetSelectedSubtitle(mediaItem, request),
            string.Empty,
            string.Empty,
            string.Empty,
            SUBTITLE_MODE,
            now,
            now + duration,
            now,
            maybeWatermark,
            Option<ChannelWatermark>.None,
            ffmpegProfile.VaapiDisplay,
            ffmpegProfile.VaapiDriver,
            ffmpegProfile.VaapiDevice,
            Option<int>.None,
            hlsRealtime,
            mediaItem is RemoteStream { IsLive: true } ? StreamInputKind.Live : StreamInputKind.Vod,
            FillerKind.None,
            inPoint,
            outPoint,
            0,
            None,
            false,
            FileSystemLayout.TranscodeTroubleshootingFolder,
            _ => { });

        return process;
    }

    private static async Task<List<Subtitle>> GetSelectedSubtitle(MediaItem mediaItem, PrepareTroubleshootingPlayback request)
    {
        if (request.SubtitleId is not null)
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

            allSubtitles.RemoveAll(s => s.Id != request.SubtitleId.Value);

            foreach (Subtitle subtitle in allSubtitles)
            {
                // pretend subtitle is forced
                subtitle.Forced = true;
                return [subtitle];
            }
        }

        return [];
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
