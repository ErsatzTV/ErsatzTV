using CliWrap;
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
            // get playout deco
            .Include(i => i.Playout)
            .ThenInclude(p => p.Deco)
            .ThenInclude(d => d.Watermark)

            // get playout templates (and deco templates/decos)
            .Include(i => i.Playout)
            .ThenInclude(p => p.Templates)
            .ThenInclude(t => t.DecoTemplate)
            .ThenInclude(t => t.Items)
            .ThenInclude(i => i.Deco)
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
            .Include(i => i.Watermark)
            .ForChannelAndTime(channel.Id, now)
            .Map(o => o.ToEither<BaseError>(new UnableToLocatePlayoutItem()))
            .BindT(item => ValidatePlayoutItemPath(dbContext, item));

        if (maybePlayoutItem.LeftAsEnumerable().Any(e => e is UnableToLocatePlayoutItem))
        {
            maybePlayoutItem = await _externalJsonPlayoutItemProvider.CheckForExternalJson(channel, now, ffprobePath);
        }

        if (maybePlayoutItem.LeftAsEnumerable().Any(e => e is UnableToLocatePlayoutItem))
        {
            Option<Playout> maybePlayout = await dbContext.Playouts
                .AsNoTracking()

                // get playout deco
                .Include(p => p.Deco)
                .ThenInclude(d => d.Watermark)

                // get playout templates (and deco templates/decos)
                .Include(p => p.Templates)
                .ThenInclude(t => t.DecoTemplate)
                .ThenInclude(t => t.Items)
                .ThenInclude(i => i.Deco)
                .ThenInclude(d => d.Watermark)
                .SelectOneAsync(p => p.ChannelId, p => p.ChannelId == channel.Id);

            foreach (Playout playout in maybePlayout)
            {
                maybePlayoutItem = await CheckForFallbackFiller(dbContext, channel, playout, now);
            }

            if (maybePlayout.IsNone)
            {
                maybePlayoutItem = await CheckForFallbackFiller(dbContext, channel, null, now);
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
                .GetValue<int>(ConfigElementKey.FFmpegGlobalWatermarkId)
                .BindT(
                    watermarkId => dbContext.ChannelWatermarks
                        .SelectOneAsync(w => w.Id, w => w.Id == watermarkId));

            Option<ChannelWatermark> playoutItemWatermark = Optional(playoutItemWithPath.PlayoutItem.Watermark);
            bool disableWatermarks = playoutItemWithPath.PlayoutItem.DisableWatermarks;
            WatermarkResult watermarkResult = GetPlayoutItemWatermark(playoutItemWithPath.PlayoutItem, now);
            switch (watermarkResult)
            {
                case InheritWatermark:
                    // do nothing, other code will fall back to channel/global
                    break;
                case DisableWatermark:
                    disableWatermarks = true;
                    break;
                case CustomWatermark watermark:
                    playoutItemWatermark = watermark.Watermark;
                    break;
            }

            if (playoutItemWithPath.PlayoutItem.MediaItem is Song song)
            {
                (videoPath, videoVersion) = await _songVideoGenerator.GenerateSongVideo(
                    song,
                    channel,
                    playoutItemWatermark,
                    maybeGlobalWatermark,
                    ffmpegPath,
                    ffprobePath,
                    cancellationToken);
            }

            if (playoutItemWithPath.PlayoutItem.MediaItem is Image)
            {
                audioPath = string.Empty;
            }

            bool saveReports = await dbContext.ConfigElements
                .GetValue<bool>(ConfigElementKey.FFmpegSaveReports)
                .Map(result => result.IfNone(false));

            DateTimeOffset start = playoutItemWithPath.PlayoutItem.StartOffset;
            DateTimeOffset finish = playoutItemWithPath.PlayoutItem.FinishOffset;
            TimeSpan inPoint = playoutItemWithPath.PlayoutItem.InPoint;
            TimeSpan outPoint = playoutItemWithPath.PlayoutItem.OutPoint;
            DateTimeOffset effectiveNow = request.StartAtZero ? start : now;
            TimeSpan duration = finish - effectiveNow;

            Command process = await _ffmpegProcessService.ForPlayoutItem(
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
                request.StartAtZero ? start : now,
                playoutItemWatermark,
                maybeGlobalWatermark,
                channel.FFmpegProfile.VaapiDriver,
                channel.FFmpegProfile.VaapiDevice,
                Optional(channel.FFmpegProfile.QsvExtraHardwareFrames),
                request.HlsRealtime,
                playoutItemWithPath.PlayoutItem.FillerKind,
                inPoint,
                outPoint,
                request.PtsOffset,
                request.TargetFramerate,
                disableWatermarks,
                _ => { });

            var result = new PlayoutItemProcessModel(process, duration, finish, true);

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
                        channel.FFmpegProfile.VaapiDriver,
                        channel.FFmpegProfile.VaapiDevice,
                        Optional(channel.FFmpegProfile.QsvExtraHardwareFrames));

                    return new PlayoutItemProcessModel(offlineProcess, maybeDuration, finish, true);
                case PlayoutItemDoesNotExistOnDisk:
                    Command doesNotExistProcess = await _ffmpegProcessService.ForError(
                        ffmpegPath,
                        channel,
                        maybeDuration,
                        error.Value,
                        request.HlsRealtime,
                        request.PtsOffset,
                        channel.FFmpegProfile.VaapiDriver,
                        channel.FFmpegProfile.VaapiDevice,
                        Optional(channel.FFmpegProfile.QsvExtraHardwareFrames));

                    return new PlayoutItemProcessModel(doesNotExistProcess, maybeDuration, finish, true);
                default:
                    Command errorProcess = await _ffmpegProcessService.ForError(
                        ffmpegPath,
                        channel,
                        maybeDuration,
                        "Channel is Offline",
                        request.HlsRealtime,
                        request.PtsOffset,
                        channel.FFmpegProfile.VaapiDriver,
                        channel.FFmpegProfile.VaapiDevice,
                        Optional(channel.FFmpegProfile.QsvExtraHardwareFrames));

                    return new PlayoutItemProcessModel(errorProcess, maybeDuration, finish, true);
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
                .Map(mm => mm.Subtitles ?? new List<Subtitle>())
                .IfNoneAsync(new List<Subtitle>()),
            Movie movie => await Optional(movie.MovieMetadata).Flatten().HeadOrNone()
                .Map(mm => mm.Subtitles ?? new List<Subtitle>())
                .IfNoneAsync(new List<Subtitle>()),
            MusicVideo musicVideo => await GetMusicVideoSubtitles(musicVideo, channel, settings),
            OtherVideo otherVideo => await Optional(otherVideo.OtherVideoMetadata).Flatten().HeadOrNone()
                .Map(mm => mm.Subtitles ?? new List<Subtitle>())
                .IfNoneAsync(new List<Subtitle>()),
            _ => new List<Subtitle>()
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
                        .IfNoneAsync(new List<Subtitle>()));
                break;
        }

        return subtitles;
    }

    private async Task<Either<BaseError, PlayoutItemWithPath>> CheckForFallbackFiller(
        TvContext dbContext,
        Channel channel,
        Playout playout,
        DateTimeOffset now)
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
                    .SelectOneAsync(w => w.Id, w => w.Id == channel.FallbackFillerId);

                // then check for global fallback
                if (maybeFallback.IsNone)
                {
                    maybeFallback = await dbContext.ConfigElements
                        .GetValue<int>(ConfigElementKey.FFmpegGlobalFallbackFillerId)
                        .BindT(fillerId => dbContext.FillerPresets.SelectOneAsync(w => w.Id, w => w.Id == fillerId));
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
                collectionKey);

            // TODO: shuffle? does it really matter since we loop anyway
            MediaItem item = items[new Random().Next(items.Count)];

            Option<TimeSpan> maybeDuration = await dbContext.PlayoutItems
                .Filter(pi => pi.Playout.ChannelId == channel.Id)
                .Filter(pi => pi.Start > now.UtcDateTime)
                .OrderBy(pi => pi.Start)
                .FirstOrDefaultAsync()
                .Map(Optional)
                .MapT(pi => pi.StartOffset - now);

            MediaVersion version = item.GetHeadVersion();

            version.MediaFiles = await dbContext.MediaFiles
                .AsNoTracking()
                .Filter(mf => mf.MediaVersionId == version.Id)
                .ToListAsync();

            version.Streams = await dbContext.MediaStreams
                .AsNoTracking()
                .Filter(ms => ms.MediaVersionId == version.Id)
                .ToListAsync();

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
                DisableWatermarks = !fallbackPreset.AllowWatermarks
            };

            return await ValidatePlayoutItemPath(dbContext, playoutItem);
        }

        return new UnableToLocatePlayoutItem();
    }

    private async Task<Either<BaseError, PlayoutItemWithPath>> ValidatePlayoutItemPath(
        TvContext dbContext,
        PlayoutItem playoutItem)
    {
        string path = await GetPlayoutItemPath(playoutItem);

        // check filesystem first
        if (_localFileSystem.FileExists(path))
        {
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
                        $"http://localhost:{Settings.ListenPort}/media/plex/{plexMediaSourceId}/{pmf.Key}");
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
                $"http://localhost:{Settings.ListenPort}/media/jellyfin/{itemId}");
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
                $"http://localhost:{Settings.ListenPort}/media/emby/{itemId}");
        }

        return new PlayoutItemDoesNotExistOnDisk(path);
    }

    private async Task<string> GetPlayoutItemPath(PlayoutItem playoutItem)
    {
        MediaVersion version = playoutItem.MediaItem.GetHeadVersion();
        MediaFile file = version.MediaFiles.Head();

        string path = file.Path;
        return playoutItem.MediaItem switch
        {
            PlexMovie plexMovie => await _plexPathReplacementService.GetReplacementPlexPath(
                plexMovie.LibraryPathId,
                path),
            PlexEpisode plexEpisode => await _plexPathReplacementService.GetReplacementPlexPath(
                plexEpisode.LibraryPathId,
                path),
            JellyfinMovie jellyfinMovie => await _jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinMovie.LibraryPathId,
                path),
            JellyfinEpisode jellyfinEpisode => await _jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinEpisode.LibraryPathId,
                path),
            EmbyMovie embyMovie => await _embyPathReplacementService.GetReplacementEmbyPath(
                embyMovie.LibraryPathId,
                path),
            EmbyEpisode embyEpisode => await _embyPathReplacementService.GetReplacementEmbyPath(
                embyEpisode.LibraryPathId,
                path),
            _ => path
        };
    }

    private WatermarkResult GetPlayoutItemWatermark(PlayoutItem playoutItem, DateTimeOffset now)
    {
        DecoEntries decoEntries = GetDecoEntries(playoutItem.Playout, now);

        // first, check deco template / active deco
        foreach (Deco templateDeco in decoEntries.TemplateDeco)
        {
            switch (templateDeco.WatermarkMode)
            {
                case DecoMode.Override:
                    if (playoutItem.FillerKind is FillerKind.None || templateDeco.UseWatermarkDuringFiller)
                    {
                        _logger.LogDebug("Watermark will come from template deco (override)");
                        return new CustomWatermark(templateDeco.Watermark);
                    }

                    _logger.LogDebug("Watermark is disabled by template deco during filler");
                    return new DisableWatermark();
                case DecoMode.Disable:
                    _logger.LogDebug("Watermark is disabled by template deco");
                    return new DisableWatermark();
                case DecoMode.Inherit:
                    _logger.LogDebug("Watermark will inherit from playout deco");
                    break;
            }
        }

        // second, check playout deco
        foreach (Deco playoutDeco in decoEntries.PlayoutDeco)
        {
            switch (playoutDeco.WatermarkMode)
            {
                case DecoMode.Override:
                    if (playoutItem.FillerKind is FillerKind.None || playoutDeco.UseWatermarkDuringFiller)
                    {
                        _logger.LogDebug("Watermark will come from playout deco (override)");
                        return new CustomWatermark(playoutDeco.Watermark);
                    }

                    _logger.LogDebug("Watermark is disabled by playout deco during filler");
                    return new DisableWatermark();
                case DecoMode.Disable:
                    _logger.LogDebug("Watermark is disabled by playout deco");
                    return new DisableWatermark();
                case DecoMode.Inherit:
                    _logger.LogDebug("Watermark will inherit from channel and/or global setting");
                    break;
            }
        }

        return new InheritWatermark();
    }

    private DeadAirFallbackResult GetDecoDeadAirFallback(Playout playout, DateTimeOffset now)
    {
        DecoEntries decoEntries = GetDecoEntries(playout, now);

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

    private static DecoEntries GetDecoEntries(Playout playout, DateTimeOffset now)
    {
        if (playout is null)
        {
            return new DecoEntries(Option<Deco>.None, Option<Deco>.None);
        }

        Option<Deco> maybePlayoutDeco = Optional(playout.Deco);
        Option<Deco> maybeTemplateDeco = Option<Deco>.None;

        Option<PlayoutTemplate> maybeActiveTemplate =
            PlayoutTemplateSelector.GetPlayoutTemplateFor(playout.Templates, now);

        foreach (PlayoutTemplate activeTemplate in maybeActiveTemplate)
        {
            Option<DecoTemplateItem> maybeItem = Optional(activeTemplate.DecoTemplate)
                .SelectMany(dt => dt.Items)
                .Find(i => i.StartTime <= now.TimeOfDay && i.EndTime == TimeSpan.Zero || i.EndTime > now.TimeOfDay);
            foreach (DecoTemplateItem item in maybeItem)
            {
                maybeTemplateDeco = Optional(item.Deco);
            }
        }

        return new DecoEntries(maybeTemplateDeco, maybePlayoutDeco);
    }

    private sealed record DecoEntries(Option<Deco> TemplateDeco, Option<Deco> PlayoutDeco);

    private abstract record WatermarkResult;

    private sealed record InheritWatermark : WatermarkResult;

    private sealed record DisableWatermark : WatermarkResult;

    private sealed record CustomWatermark(ChannelWatermark Watermark) : WatermarkResult;

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
