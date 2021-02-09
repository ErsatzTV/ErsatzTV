using System;
using System.Diagnostics;
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
        private readonly IPlayoutRepository _playoutRepository;

        public GetPlayoutItemProcessByChannelNumberHandler(
            IChannelRepository channelRepository,
            IConfigElementRepository configElementRepository,
            IPlayoutRepository playoutRepository,
            FFmpegProcessService ffmpegProcessService)
            : base(channelRepository, configElementRepository)
        {
            _playoutRepository = playoutRepository;
            _ffmpegProcessService = ffmpegProcessService;
        }

        protected override async Task<Either<BaseError, Process>> GetProcess(
            GetPlayoutItemProcessByChannelNumber _,
            Channel channel,
            string ffmpegPath)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            Option<PlayoutItem> maybePlayoutItem = await _playoutRepository.GetPlayoutItem(channel.Id, now);
            return maybePlayoutItem.Match<Either<BaseError, Process>>(
                playoutItem => _ffmpegProcessService.ForPlayoutItem(ffmpegPath, channel, playoutItem, now),
                () =>
                {
                    if (channel.FFmpegProfile.Transcode)
                    {
                        return _ffmpegProcessService.ForOfflineImage(ffmpegPath, channel);
                    }

                    return BaseError.New(
                        $"Unable to locate playout item for channel {channel.Number}; offline image is unavailable because transcoding is disabled in ffmpeg profile '{channel.FFmpegProfile.Name}'");
                });
        }
    }
}
