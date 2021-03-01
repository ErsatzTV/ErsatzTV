using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.Streaming.Queries
{
    public class
        GetPlayoutItemProcessByChannelNumberHandler : FFmpegProcessHandler<GetPlayoutItemProcessByChannelNumber>
    {
        private readonly FFmpegProcessService _ffmpegProcessService;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IPlayoutRepository _playoutRepository;

        public GetPlayoutItemProcessByChannelNumberHandler(
            IChannelRepository channelRepository,
            IConfigElementRepository configElementRepository,
            IPlayoutRepository playoutRepository,
            IMediaSourceRepository mediaSourceRepository,
            FFmpegProcessService ffmpegProcessService)
            : base(channelRepository, configElementRepository)
        {
            _playoutRepository = playoutRepository;
            _mediaSourceRepository = mediaSourceRepository;
            _ffmpegProcessService = ffmpegProcessService;
        }

        protected override async Task<Either<BaseError, Process>> GetProcess(
            GetPlayoutItemProcessByChannelNumber _,
            Channel channel,
            string ffmpegPath)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            Option<PlayoutItem> maybePlayoutItem = await _playoutRepository.GetPlayoutItem(channel.Id, now);
            return await maybePlayoutItem.Match<Task<Either<BaseError, Process>>>(
                async playoutItem =>
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

                    return _ffmpegProcessService.ForPlayoutItem(
                        ffmpegPath,
                        channel,
                        version,
                        path,
                        playoutItem.StartOffset,
                        now);
                },
                async () =>
                {
                    if (channel.FFmpegProfile.Transcode)
                    {
                        return _ffmpegProcessService.ForOfflineImage(ffmpegPath, channel);
                    }

                    return BaseError.New(
                        $"Unable to locate playout item for channel {channel.Number}; offline image is unavailable because transcoding is disabled in ffmpeg profile '{channel.FFmpegProfile.Name}'");
                });
        }

        private async Task<string> GetReplacementPlexPath(int libraryPathId, string path)
        {
            List<PlexPathReplacement> replacements =
                await _mediaSourceRepository.GetPlexPathReplacementsByLibraryId(libraryPathId);
            // TODO: this might barf mixing platforms (i.e. plex on linux, etv on windows)
            Option<PlexPathReplacement> maybeReplacement = replacements
                .SingleOrDefault(r => path.StartsWith(r.PlexPath + Path.DirectorySeparatorChar));
            return maybeReplacement.Match(
                replacement => path.Replace(replacement.PlexPath, replacement.LocalPath),
                () => path);
        }
    }
}
