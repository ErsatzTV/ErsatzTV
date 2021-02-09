using System;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Serilog;

namespace ErsatzTV.Application.Streaming.Queries
{
    public class
        GetHlsPlaylistByChannelNumberHandler : IRequestHandler<GetHlsPlaylistByChannelNumber, Either<BaseError, string>>
    {
        private readonly IChannelRepository _channelRepository;
        private readonly IPlayoutRepository _playoutRepository;

        public GetHlsPlaylistByChannelNumberHandler(
            IChannelRepository channelRepository,
            IPlayoutRepository playoutRepository)
        {
            _channelRepository = channelRepository;
            _playoutRepository = playoutRepository;
        }

        public Task<Either<BaseError, string>> Handle(
            GetHlsPlaylistByChannelNumber request,
            CancellationToken cancellationToken) =>
            ChannelMustExist(request)
                .Map(v => v.ToEither<Channel>())
                .BindT(channel => GetPlaylist(request, channel));

        private async Task<Either<BaseError, string>> GetPlaylist(
            GetHlsPlaylistByChannelNumber request,
            Channel channel)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            Option<PlayoutItem> maybePlayoutItem = await _playoutRepository.GetPlayoutItem(channel.Id, now);
            return maybePlayoutItem.Match<Either<BaseError, string>>(
                playoutItem =>
                {
                    double timeRemaining = Math.Abs((playoutItem.Finish - now).TotalSeconds);
                    return $@"#EXTM3U
#EXT-X-VERSION:3
#EXT-X-TARGETDURATION:18000
#EXTINF:{timeRemaining:F2},
{request.Scheme}://{request.Host}/ffmpeg/stream/{request.ChannelNumber}
";
                },
                () =>
                {
                    // TODO: playlist for error stream
                    Log.Logger.Error("Unable to locate playout item for m3u8");
                    return BaseError.New($"Unable to locate playout item for channel {channel.Number}");
                });
        }

        private async Task<Validation<BaseError, Channel>> ChannelMustExist(GetHlsPlaylistByChannelNumber request) =>
            (await _channelRepository.GetByNumber(request.ChannelNumber))
            .ToValidation<BaseError>($"Channel number {request.ChannelNumber} does not exist.");
    }
}
