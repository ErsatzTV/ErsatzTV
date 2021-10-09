using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Runtime;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Streaming.Queries
{
    public class GetPlayoutItemProcessByChannelNumberHandler :
        FFmpegProcessHandler<GetPlayoutItemProcessByChannelNumber>
    {
        private readonly IEmbyPathReplacementService _embyPathReplacementService;
        private readonly FFmpegProcessService _ffmpegProcessService;
        private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
        private readonly ILocalFileSystem _localFileSystem;
        private readonly IPlexPathReplacementService _plexPathReplacementService;
        private readonly IRuntimeInfo _runtimeInfo;

        public GetPlayoutItemProcessByChannelNumberHandler(
            IDbContextFactory<TvContext> dbContextFactory,
            FFmpegProcessService ffmpegProcessService,
            ILocalFileSystem localFileSystem,
            IPlexPathReplacementService plexPathReplacementService,
            IJellyfinPathReplacementService jellyfinPathReplacementService,
            IEmbyPathReplacementService embyPathReplacementService,
            IRuntimeInfo runtimeInfo)
            : base(dbContextFactory)
        {
            _ffmpegProcessService = ffmpegProcessService;
            _localFileSystem = localFileSystem;
            _plexPathReplacementService = plexPathReplacementService;
            _jellyfinPathReplacementService = jellyfinPathReplacementService;
            _embyPathReplacementService = embyPathReplacementService;
            _runtimeInfo = runtimeInfo;
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
                .ForChannelAndTime(channel.Id, now)
                .Map(o => o.ToEither<BaseError>(new UnableToLocatePlayoutItem()))
                .BindT(ValidatePlayoutItemPath);

            return await maybePlayoutItem.Match(
                async playoutItemWithPath =>
                {
                    MediaVersion version = playoutItemWithPath.PlayoutItem.MediaItem switch
                    {
                        Movie m => m.MediaVersions.Head(),
                        Episode e => e.MediaVersions.Head(),
                        MusicVideo mv => mv.MediaVersions.Head(),
                        _ => throw new ArgumentOutOfRangeException(nameof(playoutItemWithPath))
                    };

                    bool saveReports = !_runtimeInfo.IsOSPlatform(OSPlatform.Windows) && await dbContext.ConfigElements
                        .GetValue<bool>(ConfigElementKey.FFmpegSaveReports)
                        .Map(result => result.IfNone(false));

                    Option<ChannelWatermark> maybeGlobalWatermark = await dbContext.ConfigElements
                        .GetValue<int>(ConfigElementKey.FFmpegGlobalWatermarkId)
                        .BindT(
                            watermarkId => dbContext.ChannelWatermarks
                                .SelectOneAsync(w => w.Id, w => w.Id == watermarkId));

                    Option<VaapiDriver> maybeVaapiDriver = await dbContext.ConfigElements
                        .GetValue<int>(ConfigElementKey.FFmpegVaapiDriver)
                        .MapT(i => (VaapiDriver)i);

                    Process process = await _ffmpegProcessService.ForPlayoutItem(
                        ffmpegPath,
                        saveReports,
                        channel,
                        version,
                        playoutItemWithPath.Path,
                        playoutItemWithPath.PlayoutItem.StartOffset,
                        request.StartAtZero ? playoutItemWithPath.PlayoutItem.StartOffset : now,
                        maybeGlobalWatermark,
                        maybeVaapiDriver,
                        request.StartAtZero);

                    var result = new PlayoutItemProcessModel(process, playoutItemWithPath.PlayoutItem.FinishOffset);

                    return Right<BaseError, PlayoutItemProcessModel>(result);
                },
                async error =>
                {
                    var offlineTranscodeMessage =
                        $"offline image is unavailable because transcoding is disabled in ffmpeg profile '{channel.FFmpegProfile.Name}'";

                    Option<TimeSpan> maybeDuration = await Optional(channel.FFmpegProfile.Transcode)
                        .Filter(transcode => transcode)
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
                                Process errorProcess = _ffmpegProcessService.ForError(
                                    ffmpegPath,
                                    channel,
                                    maybeDuration,
                                    "Channel is Offline");


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
                                Process errorProcess = _ffmpegProcessService.ForError(
                                    ffmpegPath,
                                    channel,
                                    maybeDuration,
                                    error.Value);

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
                                Process errorProcess = _ffmpegProcessService.ForError(
                                    ffmpegPath,
                                    channel,
                                    maybeDuration,
                                    "Channel is Offline");

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
            MediaVersion version = playoutItem.MediaItem switch
            {
                Movie m => m.MediaVersions.Head(),
                Episode e => e.MediaVersions.Head(),
                MusicVideo mv => mv.MediaVersions.Head(),
                _ => throw new ArgumentOutOfRangeException(nameof(playoutItem))
            };

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
}
