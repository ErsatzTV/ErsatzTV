using System;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace ErsatzTV.Application.Streaming.Queries
{
    public class
        GetHlsPlaylistByChannelNumberHandler : IRequestHandler<GetHlsPlaylistByChannelNumber, Either<BaseError, string>>
    {
        private readonly IChannelRepository _channelRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly IPlayoutRepository _playoutRepository;

        public GetHlsPlaylistByChannelNumberHandler(
            IChannelRepository channelRepository,
            IPlayoutRepository playoutRepository,
            IMemoryCache memoryCache)
        {
            _channelRepository = channelRepository;
            _playoutRepository = playoutRepository;
            _memoryCache = memoryCache;
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
                    long index = GetIndexForChannel(channel, playoutItem);
                    double timeRemaining = Math.Abs((playoutItem.FinishOffset - now).TotalSeconds);
                    return $@"#EXTM3U
#EXT-X-VERSION:3
#EXT-X-TARGETDURATION:6
#EXT-X-MEDIA-SEQUENCE:{index}
#EXT-X-DISCONTINUITY
#EXTINF:{timeRemaining:F2},
{request.Scheme}://{request.Host}/ffmpeg/stream/{request.ChannelNumber}?index={index}&mode=hls-direct
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

        private long GetIndexForChannel(Channel channel, PlayoutItem playoutItem)
        {
            long ticks = playoutItem.Start.Ticks;
            var key = new ChannelIndexKey(channel.Id);

            long index;
            if (_memoryCache.TryGetValue(key, out ChannelIndexRecord channelRecord))
            {
                if (channelRecord.StartTicks == ticks)
                {
                    index = channelRecord.Index;
                }
                else
                {
                    index = channelRecord.Index + 1;
                    _memoryCache.Set(key, new ChannelIndexRecord(ticks, index), TimeSpan.FromDays(1));
                }
            }
            else
            {
                index = 1;
                _memoryCache.Set(key, new ChannelIndexRecord(ticks, index), TimeSpan.FromDays(1));
            }

            return index;
        }

        private record ChannelIndexKey(int ChannelId);

        private record ChannelIndexRecord(long StartTicks, long Index);
    }
}
