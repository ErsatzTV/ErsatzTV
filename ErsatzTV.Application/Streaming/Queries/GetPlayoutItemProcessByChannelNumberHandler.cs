using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Streaming.Queries
{
    public class
        GetPlayoutItemProcessByChannelNumberHandler : FFmpegProcessHandler<GetPlayoutItemProcessByChannelNumber>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly FFmpegProcessService _ffmpegProcessService;
        private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
        private readonly ILocalFileSystem _localFileSystem;
        private readonly IPlayoutRepository _playoutRepository;
        private readonly IPlexPathReplacementService _plexPathReplacementService;

        public GetPlayoutItemProcessByChannelNumberHandler(
            IChannelRepository channelRepository,
            IConfigElementRepository configElementRepository,
            IPlayoutRepository playoutRepository,
            FFmpegProcessService ffmpegProcessService,
            ILocalFileSystem localFileSystem,
            IPlexPathReplacementService plexPathReplacementService,
            IJellyfinPathReplacementService jellyfinPathReplacementService)
            : base(channelRepository, configElementRepository)
        {
            _configElementRepository = configElementRepository;
            _playoutRepository = playoutRepository;
            _ffmpegProcessService = ffmpegProcessService;
            _localFileSystem = localFileSystem;
            _plexPathReplacementService = plexPathReplacementService;
            _jellyfinPathReplacementService = jellyfinPathReplacementService;
        }

        protected override async Task<Either<BaseError, Process>> GetProcess(
            GetPlayoutItemProcessByChannelNumber _,
            Channel channel,
            string ffmpegPath)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            Either<BaseError, PlayoutItemWithPath> maybePlayoutItem = await _playoutRepository
                .GetPlayoutItem(channel.Id, now)
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

                    bool saveReports = await _configElementRepository.GetValue<bool>(ConfigElementKey.FFmpegSaveReports)
                        .Map(result => result.IfNone(false));

                    return Right<BaseError, Process>(
                        await _ffmpegProcessService.ForPlayoutItem(
                            ffmpegPath,
                            saveReports,
                            channel,
                            version,
                            playoutItemWithPath.Path,
                            playoutItemWithPath.PlayoutItem.StartOffset,
                            now));
                },
                async error =>
                {
                    var offlineTranscodeMessage =
                        $"offline image is unavailable because transcoding is disabled in ffmpeg profile '{channel.FFmpegProfile.Name}'";

                    Option<TimeSpan> maybeDuration = await Optional(channel.FFmpegProfile.Transcode)
                        .Filter(transcode => transcode)
                        .Match(
                            _ => _playoutRepository.GetNextItemStart(channel.Id, now)
                                .MapT(nextStart => nextStart - now),
                            () => Option<TimeSpan>.None.AsTask());

                    switch (error)
                    {
                        case UnableToLocatePlayoutItem:
                            if (channel.FFmpegProfile.Transcode)
                            {
                                return _ffmpegProcessService.ForError(
                                    ffmpegPath,
                                    channel,
                                    maybeDuration,
                                    "Channel is Offline");
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
                                return _ffmpegProcessService.ForError(ffmpegPath, channel, maybeDuration, error.Value);
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
                                return _ffmpegProcessService.ForError(
                                    ffmpegPath,
                                    channel,
                                    maybeDuration,
                                    "Channel is Offline");
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

            // TODO: this won't work with url streaming from plex
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
                _ => path
            };
        }

        private record PlayoutItemWithPath(PlayoutItem PlayoutItem, string Path);
    }
}
