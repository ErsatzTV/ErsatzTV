using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Streaming.Queries
{
    public class
        GetPlayoutItemProcessByChannelNumberHandler : FFmpegProcessHandler<GetPlayoutItemProcessByChannelNumber>
    {
        private readonly FFmpegProcessService _ffmpegProcessService;
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILogger<GetPlayoutItemProcessByChannelNumberHandler> _logger;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IPlayoutRepository _playoutRepository;

        public GetPlayoutItemProcessByChannelNumberHandler(
            IChannelRepository channelRepository,
            IConfigElementRepository configElementRepository,
            IPlayoutRepository playoutRepository,
            IMediaSourceRepository mediaSourceRepository,
            FFmpegProcessService ffmpegProcessService,
            ILocalFileSystem localFileSystem,
            ILogger<GetPlayoutItemProcessByChannelNumberHandler> logger)
            : base(channelRepository, configElementRepository)
        {
            _playoutRepository = playoutRepository;
            _mediaSourceRepository = mediaSourceRepository;
            _ffmpegProcessService = ffmpegProcessService;
            _localFileSystem = localFileSystem;
            _logger = logger;
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
                playoutItemWithPath =>
                {
                    MediaVersion version = playoutItemWithPath.PlayoutItem.MediaItem switch
                    {
                        Movie m => m.MediaVersions.Head(),
                        Episode e => e.MediaVersions.Head(),
                        _ => throw new ArgumentOutOfRangeException(nameof(playoutItemWithPath))
                    };

                    return Right<BaseError, Process>(
                        _ffmpegProcessService.ForPlayoutItem(
                            ffmpegPath,
                            channel,
                            version,
                            playoutItemWithPath.Path,
                            playoutItemWithPath.PlayoutItem.StartOffset,
                            now)).AsTask();
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
                _ => throw new ArgumentOutOfRangeException(nameof(playoutItem))
            };

            MediaFile file = version.MediaFiles.Head();
            string path = file.Path;
            if (playoutItem.MediaItem is PlexMovie plexMovie)
            {
                path = await GetReplacementPlexPath(plexMovie.LibraryPathId, path);
            }

            return path;
        }

        private async Task<string> GetReplacementPlexPath(int libraryPathId, string path)
        {
            List<PlexPathReplacement> replacements =
                await _mediaSourceRepository.GetPlexPathReplacementsByLibraryId(libraryPathId);
            // TODO: this might barf mixing platforms (i.e. plex on linux, etv on windows)
            Option<PlexPathReplacement> maybeReplacement = replacements
                .SingleOrDefault(r => path.StartsWith(r.PlexPath + Path.DirectorySeparatorChar));
            return maybeReplacement.Match(
                replacement =>
                {
                    string finalPath = path.Replace(replacement.PlexPath, replacement.LocalPath);
                    _logger.LogInformation(
                        "Replacing plex path {PlexPath} with {LocalPath} resulting in {FinalPath}",
                        replacement.PlexPath,
                        replacement.LocalPath,
                        finalPath);
                    return finalPath;
                },
                () => path);
        }

        private record PlayoutItemWithPath(PlayoutItem PlayoutItem, string Path);
    }
}
