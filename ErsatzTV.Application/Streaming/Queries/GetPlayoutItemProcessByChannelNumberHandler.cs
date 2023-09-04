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
using ErsatzTV.Core.Interfaces.Repositories;
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
            .Include(i => i.Watermark)
            .ForChannelAndTime(channel.Id, now)
            .Map(o => o.ToEither<BaseError>(new UnableToLocatePlayoutItem()))
            .BindT(item => ValidatePlayoutItemPath(dbContext, item));

        if (maybePlayoutItem.LeftAsEnumerable().Any(e => e is UnableToLocatePlayoutItem))
        {
            maybePlayoutItem = await CheckForFallbackFiller(dbContext, channel, now);
        }

        foreach (PlayoutItemWithPath playoutItemWithPath in maybePlayoutItem.RightToSeq())
        {
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

            if (playoutItemWithPath.PlayoutItem.MediaItem is Song song)
            {
                (videoPath, videoVersion) = await _songVideoGenerator.GenerateSongVideo(
                    song,
                    channel,
                    Optional(playoutItemWithPath.PlayoutItem.Watermark),
                    maybeGlobalWatermark,
                    ffmpegPath,
                    ffprobePath,
                    cancellationToken);
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
            var isComplete = true;

            // _logger.LogDebug("PRE Start: {Start}, Finish {Finish}", start, finish);
            // _logger.LogDebug("PRE in: {In}, out: {Out}", inPoint, outPoint);
            if (!request.HlsRealtime && duration > TimeSpan.FromMinutes(2))
            {
                finish = effectiveNow + TimeSpan.FromMinutes(2);
                outPoint = finish - start + inPoint;
                isComplete = false;

                // _logger.LogDebug("POST Start: {Start}, Finish {Finish}", start, finish);
                // _logger.LogDebug("POST in: {In}, out: {Out}", inPoint, outPoint);
            }

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
                Optional(playoutItemWithPath.PlayoutItem.Watermark),
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
                playoutItemWithPath.PlayoutItem.DisableWatermarks,
                _ => { });

            var result = new PlayoutItemProcessModel(process, duration, finish, isComplete);

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
        DateTimeOffset now)
    {
        // check for channel fallback
        Option<FillerPreset> maybeFallback = await dbContext.FillerPresets
            .SelectOneAsync(w => w.Id, w => w.Id == channel.FallbackFillerId);

        // then check for global fallback
        if (maybeFallback.IsNone)
        {
            maybeFallback = await dbContext.ConfigElements
                .GetValue<int>(ConfigElementKey.FFmpegGlobalFallbackFillerId)
                .BindT(fillerId => dbContext.FillerPresets.SelectOneAsync(w => w.Id, w => w.Id == fillerId));
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

    private sealed record PlayoutItemWithPath(PlayoutItem PlayoutItem, string Path);
}
