﻿using CliWrap;
using Dapper;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.State;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Streaming;

public class GetPlayoutItemProcessByChannelNumberHandler : FFmpegProcessHandler<GetPlayoutItemProcessByChannelNumber>
{
    private readonly IArtistRepository _artistRepository;
    private readonly IEmbyPathReplacementService _embyPathReplacementService;
    private readonly IExternalJsonPlayoutItemProvider _externalJsonPlayoutItemProvider;
    private readonly IFFmpegProcessService _ffmpegProcessService;
    private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<GetPlayoutItemProcessByChannelNumberHandler> _logger;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;
    private readonly IMusicVideoCreditsGenerator _musicVideoCreditsGenerator;
    private readonly IWatermarkSelector _watermarkSelector;
    private readonly IPlexPathReplacementService _plexPathReplacementService;
    private readonly ISongVideoGenerator _songVideoGenerator;
    private readonly ITelevisionRepository _televisionRepository;

    public GetPlayoutItemProcessByChannelNumberHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IFFmpegProcessService ffmpegProcessService,
        ILocalFileSystem localFileSystem,
        IExternalJsonPlayoutItemProvider externalJsonPlayoutItemProvider,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService,
        IMediaCollectionRepository mediaCollectionRepository,
        ITelevisionRepository televisionRepository,
        IArtistRepository artistRepository,
        ISongVideoGenerator songVideoGenerator,
        IMusicVideoCreditsGenerator musicVideoCreditsGenerator,
        IWatermarkSelector watermarkSelector,
        ILogger<GetPlayoutItemProcessByChannelNumberHandler> logger)
        : base(dbContextFactory)
    {
        _ffmpegProcessService = ffmpegProcessService;
        _localFileSystem = localFileSystem;
        _externalJsonPlayoutItemProvider = externalJsonPlayoutItemProvider;
        _plexPathReplacementService = plexPathReplacementService;
        _jellyfinPathReplacementService = jellyfinPathReplacementService;
        _embyPathReplacementService = embyPathReplacementService;
        _mediaCollectionRepository = mediaCollectionRepository;
        _televisionRepository = televisionRepository;
        _artistRepository = artistRepository;
        _songVideoGenerator = songVideoGenerator;
        _musicVideoCreditsGenerator = musicVideoCreditsGenerator;
        _watermarkSelector = watermarkSelector;
        _logger = logger;
    }

    protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        GetPlayoutItemProcessByChannelNumber request,
        Channel channel,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = request.Now;

        Either<BaseError, PlayoutItemWithPath> maybePlayoutItem = await dbContext.PlayoutItems
            .AsNoTracking()

            // get playout deco
            .Include(i => i.Playout)
            .ThenInclude(p => p.Deco)
            .ThenInclude(d => d.DecoWatermarks)
            .ThenInclude(d => d.Watermark)

            // get graphics elements
            .Include(i => i.PlayoutItemGraphicsElements)
            .ThenInclude(pige => pige.GraphicsElement)

            // get playout templates (and deco templates/decos)
            .Include(i => i.Playout)
            .ThenInclude(p => p.Templates)
            .ThenInclude(t => t.DecoTemplate)
            .ThenInclude(t => t.Items)
            .ThenInclude(i => i.Deco)
            .ThenInclude(d => d.DecoWatermarks)
            .ThenInclude(d => d.Watermark)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Episode).EpisodeMetadata)
            .ThenInclude(em => em.Subtitles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Episode).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Episode).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Episode).Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Movie).MovieMetadata)
            .ThenInclude(mm => mm.Subtitles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Movie).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Movie).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Subtitles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Artists)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Studios)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as MusicVideo).MusicVideoMetadata)
            .ThenInclude(mvm => mvm.Directors)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as MusicVideo).Artist)
            .ThenInclude(mv => mv.ArtistMetadata)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as OtherVideo).OtherVideoMetadata)
            .ThenInclude(ovm => ovm.Subtitles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as OtherVideo).MediaVersions)
            .ThenInclude(ov => ov.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as OtherVideo).MediaVersions)
            .ThenInclude(ov => ov.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Song).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Song).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Song).SongMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Image).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Image).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Image).ImageMetadata)
            .Include(i => i.Watermarks)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as RemoteStream).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as RemoteStream).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as RemoteStream).RemoteStreamMetadata)
            .Include(i => i.Watermarks)
            .ForChannelAndTime(channel.Id, now)
            .Map(o => o.ToEither<BaseError>(new UnableToLocatePlayoutItem()))
            .BindT(item => ValidatePlayoutItemPath(dbContext, item, cancellationToken));

        if (maybePlayoutItem.LeftAsEnumerable().Any(e => e is UnableToLocatePlayoutItem))
        {
            maybePlayoutItem = await _externalJsonPlayoutItemProvider.CheckForExternalJson(
                channel,
                now,
                ffprobePath,
                cancellationToken);
        }

        if (maybePlayoutItem.LeftAsEnumerable().Any(e => e is UnableToLocatePlayoutItem))
        {
            Option<Playout> maybePlayout = await dbContext.Playouts
                .AsNoTracking()

                // get playout deco
                .Include(p => p.Deco)
                .ThenInclude(d => d.DecoWatermarks)
                .ThenInclude(d => d.Watermark)

                // get playout templates (and deco templates/decos)
                .Include(p => p.Templates)
                .ThenInclude(t => t.DecoTemplate)
                .ThenInclude(t => t.Items)
                .ThenInclude(i => i.Deco)
                .ThenInclude(d => d.DecoWatermarks)
                .ThenInclude(d => d.Watermark)
                .SelectOneAsync(p => p.ChannelId, p => p.ChannelId == channel.Id, cancellationToken);

            foreach (Playout playout in maybePlayout)
            {
                maybePlayoutItem = await CheckForFallbackFiller(dbContext, channel, playout, now, cancellationToken);
            }

            if (maybePlayout.IsNone)
            {
                maybePlayoutItem = await CheckForFallbackFiller(dbContext, channel, null, now, cancellationToken);
            }
        }

        foreach (PlayoutItemWithPath playoutItemWithPath in maybePlayoutItem.RightToSeq())
        {
            try
            {
                PlayoutItemViewModel viewModel = Mapper.ProjectToViewModel(playoutItemWithPath.PlayoutItem);
                if (!string.IsNullOrWhiteSpace(viewModel.Title))
                {
                    _logger.LogDebug(
                        "Found playout item {Title} with path {Path}",
                        viewModel.Title,
                        playoutItemWithPath.Path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get playout item title");
            }

            MediaVersion version = playoutItemWithPath.PlayoutItem.MediaItem.GetHeadVersion();

            string videoPath = playoutItemWithPath.Path;
            MediaVersion videoVersion = version;

            string audioPath = playoutItemWithPath.Path;
            MediaVersion audioVersion = version;

            Option<ChannelWatermark> maybeGlobalWatermark = await dbContext.ConfigElements
                .GetValue<int>(ConfigElementKey.FFmpegGlobalWatermarkId, cancellationToken)
                .BindT(watermarkId => dbContext.ChannelWatermarks
                    .SelectOneAsync(w => w.Id, w => w.Id == watermarkId, cancellationToken));

            List<WatermarkOptions> watermarks = _watermarkSelector.SelectWatermarks(
                maybeGlobalWatermark,
                channel,
                playoutItemWithPath.PlayoutItem,
                now);

            if (playoutItemWithPath.PlayoutItem.MediaItem is Song song)
            {
                (videoPath, videoVersion) = await _songVideoGenerator.GenerateSongVideo(
                    song,
                    channel,
                    ffmpegPath,
                    ffprobePath,
                    cancellationToken);

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

            if (playoutItemWithPath.PlayoutItem.MediaItem is Image)
            {
                audioPath = string.Empty;
            }

            bool saveReports = await dbContext.ConfigElements
                .GetValue<bool>(ConfigElementKey.FFmpegSaveReports, cancellationToken)
                .Map(result => result.IfNone(false));

            DateTimeOffset start = playoutItemWithPath.PlayoutItem.StartOffset;
            DateTimeOffset finish = playoutItemWithPath.PlayoutItem.FinishOffset;
            TimeSpan inPoint = playoutItemWithPath.PlayoutItem.InPoint;
            TimeSpan outPoint = playoutItemWithPath.PlayoutItem.OutPoint;
            DateTimeOffset effectiveNow = request.StartAtZero ? start : now;
            TimeSpan duration = finish - effectiveNow;

            _logger.LogDebug(
                "S: {Start}, F: {Finish}, In: {InPoint}, Out: {OutPoint}, EffNow: {EffectiveNow}, Dur: {Duration}",
                start,
                finish,
                inPoint,
                outPoint,
                effectiveNow,
                duration);

            PlayoutItemResult playoutItemResult = await _ffmpegProcessService.ForPlayoutItem(
                ffmpegPath,
                ffprobePath,
                saveReports,
                channel,
                videoVersion,
                new MediaItemAudioVersion(playoutItemWithPath.PlayoutItem.MediaItem, audioVersion),
                videoPath,
                audioPath,
                settings => GetSubtitles(playoutItemWithPath, channel, settings),
                playoutItemWithPath.PlayoutItem.PreferredAudioLanguageCode ?? channel.PreferredAudioLanguageCode,
                playoutItemWithPath.PlayoutItem.PreferredAudioTitle ?? channel.PreferredAudioTitle,
                playoutItemWithPath.PlayoutItem.PreferredSubtitleLanguageCode ?? channel.PreferredSubtitleLanguageCode,
                playoutItemWithPath.PlayoutItem.SubtitleMode ?? channel.SubtitleMode,
                start,
                finish,
                effectiveNow,
                watermarks,
                playoutItemWithPath.PlayoutItem.PlayoutItemGraphicsElements,
                channel.FFmpegProfile.VaapiDisplay,
                channel.FFmpegProfile.VaapiDriver,
                channel.FFmpegProfile.VaapiDevice,
                Optional(channel.FFmpegProfile.QsvExtraHardwareFrames),
                hlsRealtime: request.HlsRealtime,
                playoutItemWithPath.PlayoutItem.MediaItem is RemoteStream { IsLive: true }
                    ? StreamInputKind.Live
                    : StreamInputKind.Vod,
                playoutItemWithPath.PlayoutItem.FillerKind,
                inPoint,
                outPoint,
                request.ChannelStartTime,
                request.PtsOffset,
                request.TargetFramerate,
                Option<string>.None,
                _ => { },
                cancellationToken);

            var result = new PlayoutItemProcessModel(
                playoutItemResult.Process,
                playoutItemResult.GraphicsEngineContext,
                duration,
                finish,
                true);

            return Right<BaseError, PlayoutItemProcessModel>(result);
        }

        foreach (BaseError error in maybePlayoutItem.LeftToSeq())
        {
            Option<TimeSpan> maybeDuration = await dbContext.PlayoutItems
                .Filter(pi => pi.Playout.ChannelId == channel.Id)
                .Filter(pi => pi.Start > now.UtcDateTime)
                .OrderBy(pi => pi.Start)
                .FirstOrDefaultAsync(cancellationToken)
                .Map(Optional)
                .MapT(pi => pi.StartOffset - now);

            DateTimeOffset finish = maybeDuration.Match(d => now.Add(d), () => now);

            _logger.LogWarning(
                "Error locating playout item {@Error}. Will display error from {Start} to {Finish}",
                error,
                now,
                finish);

            switch (error)
            {
                case UnableToLocatePlayoutItem:
                    Command offlineProcess = await _ffmpegProcessService.ForError(
                        ffmpegPath,
                        channel,
                        maybeDuration,
                        "Channel is Offline",
                        request.HlsRealtime,
                        request.PtsOffset,
                        channel.FFmpegProfile.VaapiDisplay,
                        channel.FFmpegProfile.VaapiDriver,
                        channel.FFmpegProfile.VaapiDevice,
                        Optional(channel.FFmpegProfile.QsvExtraHardwareFrames));

                    return new PlayoutItemProcessModel(
                        offlineProcess,
                        Option<GraphicsEngineContext>.None,
                        maybeDuration,
                        finish,
                        true);
                case PlayoutItemDoesNotExistOnDisk:
                    Command doesNotExistProcess = await _ffmpegProcessService.ForError(
                        ffmpegPath,
                        channel,
                        maybeDuration,
                        error.Value,
                        request.HlsRealtime,
                        request.PtsOffset,
                        channel.FFmpegProfile.VaapiDisplay,
                        channel.FFmpegProfile.VaapiDriver,
                        channel.FFmpegProfile.VaapiDevice,
                        Optional(channel.FFmpegProfile.QsvExtraHardwareFrames));

                    return new PlayoutItemProcessModel(
                        doesNotExistProcess,
                        Option<GraphicsEngineContext>.None,
                        maybeDuration,
                        finish,
                        true);
                default:
                    Command errorProcess = await _ffmpegProcessService.ForError(
                        ffmpegPath,
                        channel,
                        maybeDuration,
                        "Channel is Offline",
                        request.HlsRealtime,
                        request.PtsOffset,
                        channel.FFmpegProfile.VaapiDisplay,
                        channel.FFmpegProfile.VaapiDriver,
                        channel.FFmpegProfile.VaapiDevice,
                        Optional(channel.FFmpegProfile.QsvExtraHardwareFrames));

                    return new PlayoutItemProcessModel(
                        errorProcess,
                        Option<GraphicsEngineContext>.None,
                        maybeDuration,
                        finish,
                        true);
            }
        }

        return BaseError.New($"Unexpected error locating playout item for channel {channel.Number}");
    }

    private async Task<List<Subtitle>> GetSubtitles(
        PlayoutItemWithPath playoutItemWithPath,
        Channel channel,
        FFmpegPlaybackSettings settings)
    {
        List<Subtitle> allSubtitles = playoutItemWithPath.PlayoutItem.MediaItem switch
        {
            Episode episode => await Optional(episode.EpisodeMetadata).Flatten().HeadOrNone()
                .Map(mm => mm.Subtitles ?? [])
                .IfNoneAsync([]),
            Movie movie => await Optional(movie.MovieMetadata).Flatten().HeadOrNone()
                .Map(mm => mm.Subtitles ?? [])
                .IfNoneAsync([]),
            MusicVideo musicVideo => await GetMusicVideoSubtitles(musicVideo, channel, settings),
            OtherVideo otherVideo => await Optional(otherVideo.OtherVideoMetadata).Flatten().HeadOrNone()
                .Map(mm => mm.Subtitles ?? [])
                .IfNoneAsync([]),
            _ => []
        };

        bool isMediaServer = playoutItemWithPath.PlayoutItem.MediaItem is PlexMovie or PlexEpisode or
            JellyfinMovie or JellyfinEpisode or EmbyMovie or EmbyEpisode;

        if (isMediaServer)
        {
            // closed captions are currently unsupported
            allSubtitles.RemoveAll(s => s.Codec == "eia_608");
        }

        return allSubtitles;
    }

    private async Task<List<Subtitle>> GetMusicVideoSubtitles(
        MusicVideo musicVideo,
        Channel channel,
        FFmpegPlaybackSettings settings)
    {
        var subtitles = new List<Subtitle>();

        switch (channel.MusicVideoCreditsMode)
        {
            case ChannelMusicVideoCreditsMode.GenerateSubtitles:
                var fileWithExtension = $"{channel.MusicVideoCreditsTemplate}.sbntxt";
                if (!string.IsNullOrWhiteSpace(fileWithExtension))
                {
                    subtitles.AddRange(
                        await _musicVideoCreditsGenerator.GenerateCreditsSubtitleFromTemplate(
                            musicVideo,
                            channel.FFmpegProfile,
                            settings,
                            Path.Combine(FileSystemLayout.MusicVideoCreditsTemplatesFolder, fileWithExtension)));
                }
                else
                {
                    _logger.LogWarning(
                        "Music video credits template {Template} does not exist; falling back to built-in template",
                        fileWithExtension);

                    subtitles.AddRange(
                        await _musicVideoCreditsGenerator.GenerateCreditsSubtitle(musicVideo, channel.FFmpegProfile));
                }

                break;
            case ChannelMusicVideoCreditsMode.None:
            default:
                subtitles.AddRange(
                    await Optional(musicVideo.MusicVideoMetadata).Flatten().HeadOrNone()
                        .Map(mm => mm.Subtitles)
                        .IfNoneAsync([]));
                break;
        }

        return subtitles;
    }

    private async Task<Either<BaseError, PlayoutItemWithPath>> CheckForFallbackFiller(
        TvContext dbContext,
        Channel channel,
        Playout playout,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        Option<FillerPreset> maybeFallback = Option<FillerPreset>.None;

        DeadAirFallbackResult decoDeadAirFallback = GetDecoDeadAirFallback(playout, now);
        switch (decoDeadAirFallback)
        {
            case CustomDeadAirFallback custom:
                maybeFallback = new FillerPreset
                {
                    // always allow watermarks here
                    // deco settings will disable watermarks if appropriate
                    AllowWatermarks = true,

                    CollectionType = custom.CollectionType,
                    CollectionId = custom.CollectionId,
                    MediaItemId = custom.MediaItemId,
                    MultiCollectionId = custom.MultiCollectionId,
                    SmartCollectionId = custom.SmartCollectionId
                };
                break;
            case DisableDeadAirFallback:
                // do nothing
                break;
            case InheritDeadAirFallback:
                // check for channel fallback
                maybeFallback = await dbContext.FillerPresets
                    .SelectOneAsync(w => w.Id, w => w.Id == channel.FallbackFillerId, cancellationToken);

                // then check for global fallback
                if (maybeFallback.IsNone)
                {
                    maybeFallback = await dbContext.ConfigElements
                        .GetValue<int>(ConfigElementKey.FFmpegGlobalFallbackFillerId, cancellationToken)
                        .BindT(fillerId => dbContext.FillerPresets.SelectOneAsync(
                            w => w.Id,
                            w => w.Id == fillerId,
                            cancellationToken));
                }

                break;
        }


        foreach (FillerPreset fallbackPreset in maybeFallback)
        {
            // turn this into a playout item

            var collectionKey = CollectionKey.ForFillerPreset(fallbackPreset);
            List<MediaItem> items = await MediaItemsForCollection.Collect(
                _mediaCollectionRepository,
                _televisionRepository,
                _artistRepository,
                collectionKey,
                cancellationToken);

            // TODO: shuffle? does it really matter since we loop anyway
            MediaItem item = items[new Random().Next(items.Count)];

            Option<TimeSpan> maybeDuration = await dbContext.PlayoutItems
                .Filter(pi => pi.Playout.ChannelId == channel.Id)
                .Filter(pi => pi.Start > now.UtcDateTime)
                .OrderBy(pi => pi.Start)
                .FirstOrDefaultAsync(cancellationToken)
                .Map(Optional)
                .MapT(pi => pi.StartOffset - now);

            MediaVersion version = item.GetHeadVersion();

            version.MediaFiles = await dbContext.MediaFiles
                .AsNoTracking()
                .Filter(mf => mf.MediaVersionId == version.Id)
                .ToListAsync(cancellationToken);

            version.Streams = await dbContext.MediaStreams
                .AsNoTracking()
                .Filter(ms => ms.MediaVersionId == version.Id)
                .ToListAsync(cancellationToken);

            DateTimeOffset finish = maybeDuration.Match(
                // next playout item exists
                // loop until it starts
                now.Add,
                // no next playout item exists
                // loop for 5 minutes if less than 30s, otherwise play full item
                () => version.Duration < TimeSpan.FromSeconds(30)
                    ? now.AddMinutes(5)
                    : now.Add(version.Duration));

            var playoutItem = new PlayoutItem
            {
                MediaItem = item,
                MediaItemId = item.Id,
                Start = now.UtcDateTime,
                Finish = finish.UtcDateTime,
                FillerKind = FillerKind.Fallback,
                InPoint = TimeSpan.Zero,
                OutPoint = version.Duration,
                DisableWatermarks = !fallbackPreset.AllowWatermarks,
                Watermarks = [],
                PlayoutItemWatermarks = [],
                GraphicsElements = [],
                PlayoutItemGraphicsElements = []
            };

            return await ValidatePlayoutItemPath(dbContext, playoutItem, cancellationToken);
        }

        return new UnableToLocatePlayoutItem();
    }

    private async Task<Either<BaseError, PlayoutItemWithPath>> ValidatePlayoutItemPath(
        TvContext dbContext,
        PlayoutItem playoutItem,
        CancellationToken cancellationToken)
    {
        string path = await GetPlayoutItemPath(playoutItem, cancellationToken);

        // check filesystem first
        if (_localFileSystem.FileExists(path))
        {
            if (playoutItem.MediaItem is RemoteStream remoteStream)
            {
                path = !string.IsNullOrWhiteSpace(remoteStream.Url)
                    ? remoteStream.Url
                    : $"http://localhost:{Settings.StreamingPort}/ffmpeg/remote-stream/{remoteStream.Id}";
            }

            return new PlayoutItemWithPath(playoutItem, path);
        }

        // attempt to remotely stream plex
        MediaFile file = playoutItem.MediaItem.GetHeadVersion().MediaFiles.Head();
        switch (file)
        {
            case PlexMediaFile pmf:
                Option<int> maybeId = await dbContext.Connection.QuerySingleOrDefaultAsync<int>(
                        @"SELECT PMS.Id FROM PlexMediaSource PMS
                  INNER JOIN Library L on PMS.Id = L.MediaSourceId
                  INNER JOIN LibraryPath LP on L.Id = LP.LibraryId
                  WHERE LP.Id = @LibraryPathId",
                        new { playoutItem.MediaItem.LibraryPathId })
                    .Map(Optional);

                foreach (int plexMediaSourceId in maybeId)
                {
                    _logger.LogDebug(
                        "Attempting to stream Plex file {PlexFileName} using key {PlexKey}",
                        pmf.Path,
                        pmf.Key);

                    return new PlayoutItemWithPath(
                        playoutItem,
                        $"http://localhost:{Settings.StreamingPort}/media/plex/{plexMediaSourceId}/{pmf.Key}");
                }

                break;
        }

        // attempt to remotely stream jellyfin
        Option<string> jellyfinItemId = playoutItem.MediaItem switch
        {
            JellyfinEpisode e => e.ItemId,
            JellyfinMovie m => m.ItemId,
            _ => None
        };

        foreach (string itemId in jellyfinItemId)
        {
            return new PlayoutItemWithPath(
                playoutItem,
                $"http://localhost:{Settings.StreamingPort}/media/jellyfin/{itemId}");
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
            return new PlayoutItemWithPath(
                playoutItem,
                $"http://localhost:{Settings.StreamingPort}/media/emby/{itemId}");
        }

        return new PlayoutItemDoesNotExistOnDisk(path);
    }

    private async Task<string> GetPlayoutItemPath(PlayoutItem playoutItem, CancellationToken cancellationToken)
    {
        MediaVersion version = playoutItem.MediaItem.GetHeadVersion();
        MediaFile file = version.MediaFiles.Head();

        string path = file.Path;
        return playoutItem.MediaItem switch
        {
            PlexMovie plexMovie => await _plexPathReplacementService.GetReplacementPlexPath(
                plexMovie.LibraryPathId,
                path,
                cancellationToken),
            PlexEpisode plexEpisode => await _plexPathReplacementService.GetReplacementPlexPath(
                plexEpisode.LibraryPathId,
                path,
                cancellationToken),
            JellyfinMovie jellyfinMovie => await _jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinMovie.LibraryPathId,
                path,
                cancellationToken),
            JellyfinEpisode jellyfinEpisode => await _jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinEpisode.LibraryPathId,
                path,
                cancellationToken),
            EmbyMovie embyMovie => await _embyPathReplacementService.GetReplacementEmbyPath(
                embyMovie.LibraryPathId,
                path,
                cancellationToken),
            EmbyEpisode embyEpisode => await _embyPathReplacementService.GetReplacementEmbyPath(
                embyEpisode.LibraryPathId,
                path,
                cancellationToken),
            _ => path
        };
    }

    private DeadAirFallbackResult GetDecoDeadAirFallback(Playout playout, DateTimeOffset now)
    {
        DecoEntries decoEntries = DecoSelector.GetDecoEntries(playout, now);

        // first, check deco template / active deco
        foreach (Deco templateDeco in decoEntries.TemplateDeco)
        {
            switch (templateDeco.DeadAirFallbackMode)
            {
                case DecoMode.Override:
                    _logger.LogDebug("Dead air fallback will come from template deco (override)");
                    return new CustomDeadAirFallback(
                        templateDeco.DeadAirFallbackCollectionType,
                        templateDeco.DeadAirFallbackCollectionId,
                        templateDeco.DeadAirFallbackMediaItemId,
                        templateDeco.DeadAirFallbackMultiCollectionId,
                        templateDeco.DeadAirFallbackSmartCollectionId);
                case DecoMode.Disable:
                    _logger.LogDebug("Dead air fallback is disabled by template deco");
                    return new DisableDeadAirFallback();
                case DecoMode.Inherit:
                    _logger.LogDebug("Dead air fallback will inherit from playout deco");
                    break;
            }
        }

        // second, check playout deco
        foreach (Deco playoutDeco in decoEntries.PlayoutDeco)
        {
            switch (playoutDeco.DeadAirFallbackMode)
            {
                case DecoMode.Override:
                    _logger.LogDebug("Dead air fallback will come from playout deco (override)");
                    return new CustomDeadAirFallback(
                        playoutDeco.DeadAirFallbackCollectionType,
                        playoutDeco.DeadAirFallbackCollectionId,
                        playoutDeco.DeadAirFallbackMediaItemId,
                        playoutDeco.DeadAirFallbackMultiCollectionId,
                        playoutDeco.DeadAirFallbackSmartCollectionId);
                case DecoMode.Disable:
                    _logger.LogDebug("Dead air fallback is disabled by playout deco");
                    return new DisableDeadAirFallback();
                case DecoMode.Inherit:
                    _logger.LogDebug("Dead air fallback will inherit from channel and/or global setting");
                    break;
            }
        }

        return new InheritDeadAirFallback();
    }

    private abstract record DeadAirFallbackResult;

    private sealed record InheritDeadAirFallback : DeadAirFallbackResult;

    private sealed record DisableDeadAirFallback : DeadAirFallbackResult;

    private sealed record CustomDeadAirFallback(
        ProgramScheduleItemCollectionType CollectionType,
        int? CollectionId,
        int? MediaItemId,
        int? MultiCollectionId,
        int? SmartCollectionId) : DeadAirFallbackResult;
}
