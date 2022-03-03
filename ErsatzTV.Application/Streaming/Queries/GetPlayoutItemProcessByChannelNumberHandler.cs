using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Streaming;

public class GetPlayoutItemProcessByChannelNumberHandler :
    FFmpegProcessHandler<GetPlayoutItemProcessByChannelNumber>
{
    private readonly IEmbyPathReplacementService _embyPathReplacementService;
    private readonly IMediaCollectionRepository _mediaCollectionRepository;
    private readonly ITelevisionRepository _televisionRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
    private readonly IFFmpegProcessServiceFactory _ffmpegProcessServiceFactory;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly IPlexPathReplacementService _plexPathReplacementService;
    private readonly ISongVideoGenerator _songVideoGenerator;

    public GetPlayoutItemProcessByChannelNumberHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IFFmpegProcessServiceFactory ffmpegProcessServiceFactory,
        ILocalFileSystem localFileSystem,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService,
        IMediaCollectionRepository mediaCollectionRepository,
        ITelevisionRepository televisionRepository,
        IArtistRepository artistRepository,
        ISongVideoGenerator songVideoGenerator)
        : base(dbContextFactory)
    {
        _ffmpegProcessServiceFactory = ffmpegProcessServiceFactory;
        _localFileSystem = localFileSystem;
        _plexPathReplacementService = plexPathReplacementService;
        _jellyfinPathReplacementService = jellyfinPathReplacementService;
        _embyPathReplacementService = embyPathReplacementService;
        _mediaCollectionRepository = mediaCollectionRepository;
        _televisionRepository = televisionRepository;
        _artistRepository = artistRepository;
        _songVideoGenerator = songVideoGenerator;
    }

    protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        GetPlayoutItemProcessByChannelNumber request,
        Channel channel,
        string ffmpegPath)
    {
        DateTimeOffset now = request.Now;
            
        Either<BaseError, PlayoutItemWithPath> maybePlayoutItem = await dbContext.PlayoutItems
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Episode).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Episode).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Movie).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as Movie).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaItem)
            .ThenInclude(mi => (mi as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.Streams)
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
            .ForChannelAndTime(channel.Id, now)
            .Map(o => o.ToEither<BaseError>(new UnableToLocatePlayoutItem()))
            .BindT(ValidatePlayoutItemPath);

        if (maybePlayoutItem.LeftAsEnumerable().Any(e => e is UnableToLocatePlayoutItem))
        {
            maybePlayoutItem = await CheckForFallbackFiller(dbContext, channel, now);
        }

        IFFmpegProcessService ffmpegProcessService = await _ffmpegProcessServiceFactory.GetService();

        return await maybePlayoutItem.Match(
            async playoutItemWithPath =>
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
                        maybeGlobalWatermark,
                        ffmpegPath);
                }

                bool saveReports = await dbContext.ConfigElements
                    .GetValue<bool>(ConfigElementKey.FFmpegSaveReports)
                    .Map(result => result.IfNone(false));

                Process process = await ffmpegProcessService.ForPlayoutItem(
                    ffmpegPath,
                    saveReports,
                    channel,
                    videoVersion,
                    audioVersion,
                    videoPath,
                    audioPath,
                    playoutItemWithPath.PlayoutItem.StartOffset,
                    playoutItemWithPath.PlayoutItem.FinishOffset,
                    request.StartAtZero ? playoutItemWithPath.PlayoutItem.StartOffset : now,
                    maybeGlobalWatermark,
                    channel.FFmpegProfile.VaapiDriver,
                    channel.FFmpegProfile.VaapiDevice,
                    request.HlsRealtime,
                    playoutItemWithPath.PlayoutItem.FillerKind,
                    playoutItemWithPath.PlayoutItem.InPoint,
                    playoutItemWithPath.PlayoutItem.OutPoint,
                    request.PtsOffset,
                    request.TargetFramerate);

                var result = new PlayoutItemProcessModel(process, playoutItemWithPath.PlayoutItem.FinishOffset);

                return Right<BaseError, PlayoutItemProcessModel>(result);
            },
            async error =>
            {
                var offlineTranscodeMessage =
                    $"offline image is unavailable because transcoding is disabled in ffmpeg profile '{channel.FFmpegProfile.Name}'";

                Option<TimeSpan> maybeDuration = await Optional(channel.FFmpegProfile.Transcode)
                    .Where(transcode => transcode)
                    .Match(
                        _ => dbContext.PlayoutItems
                            .Filter(pi => pi.Playout.ChannelId == channel.Id)
                            .Filter(pi => pi.Start > now.UtcDateTime)
                            .OrderBy(pi => pi.Start)
                            .FirstOrDefaultAsync()
                            .Map(Optional)
                            .MapT(pi => pi.StartOffset - now),
                        () => Option<TimeSpan>.None.AsTask());

                DateTimeOffset finish = maybeDuration.Match(d => now.Add(d), () => now);

                switch (error)
                {
                    case UnableToLocatePlayoutItem:
                        if (channel.FFmpegProfile.Transcode)
                        {
                            Process errorProcess = await ffmpegProcessService.ForError(
                                ffmpegPath,
                                channel,
                                maybeDuration,
                                "Channel is Offline",
                                request.HlsRealtime,
                                request.PtsOffset);
                                
                            return new PlayoutItemProcessModel(errorProcess, finish);
                        }
                        else
                        {
                            var message =
                                $"Unable to locate playout item for channel {channel.Number}; {offlineTranscodeMessage}";

                            return BaseError.New(message);
                        }
                    case PlayoutItemDoesNotExistOnDisk:
                        if (channel.FFmpegProfile.Transcode)
                        {
                            Process errorProcess = await ffmpegProcessService.ForError(
                                ffmpegPath,
                                channel,
                                maybeDuration,
                                error.Value,
                                request.HlsRealtime,
                                request.PtsOffset);

                            return new PlayoutItemProcessModel(errorProcess, finish);
                        }
                        else
                        {
                            var message =
                                $"Playout item does not exist on disk for channel {channel.Number}; {offlineTranscodeMessage}";

                            return BaseError.New(message);
                        }
                    default:
                        if (channel.FFmpegProfile.Transcode)
                        {
                            Process errorProcess = await ffmpegProcessService.ForError(
                                ffmpegPath,
                                channel,
                                maybeDuration,
                                "Channel is Offline",
                                request.HlsRealtime,
                                request.PtsOffset);

                            return new PlayoutItemProcessModel(errorProcess, finish);
                        }
                        else
                        {
                            var message =
                                $"Unexpected error locating playout item for channel {channel.Number}; {offlineTranscodeMessage}";

                            return BaseError.New(message);
                        }
                }
            });
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
                
            Option<TimeSpan> maybeDuration = await Optional(channel.FFmpegProfile.Transcode)
                .Where(transcode => transcode)
                .Match(
                    _ => dbContext.PlayoutItems
                        .Filter(pi => pi.Playout.ChannelId == channel.Id)
                        .Filter(pi => pi.Start > now.UtcDateTime)
                        .OrderBy(pi => pi.Start)
                        .FirstOrDefaultAsync()
                        .Map(Optional)
                        .MapT(pi => pi.StartOffset - now),
                    () => Option<TimeSpan>.None.AsTask());

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
                OutPoint = version.Duration
            };
                
            return await ValidatePlayoutItemPath(playoutItem);
        }

        return new UnableToLocatePlayoutItem();
    }

    private async Task<Either<BaseError, PlayoutItemWithPath>> ValidatePlayoutItemPath(PlayoutItem playoutItem)
    {
        string path = await GetPlayoutItemPath(playoutItem);

        if (_localFileSystem.FileExists(path))
        {
            return new PlayoutItemWithPath(playoutItem, path);
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

    private record PlayoutItemWithPath(PlayoutItem PlayoutItem, string Path);
}