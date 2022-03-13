using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ErsatzTV.Application.Streaming;

public class GetHlsPlaylistByChannelNumberHandler :
    IRequestHandler<GetHlsPlaylistByChannelNumber, Either<BaseError, string>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMemoryCache _memoryCache;

    public GetHlsPlaylistByChannelNumberHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMemoryCache memoryCache)
    {
        _dbContextFactory = dbContextFactory;
        _memoryCache = memoryCache;
    }

    public async Task<Either<BaseError, string>> Handle(
        GetHlsPlaylistByChannelNumber request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.Now;
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request, now);
        return await validation.Apply(parameters => GetPlaylist(dbContext, request, parameters, now));
    }

    private Task<string> GetPlaylist(
        TvContext dbContext,
        GetHlsPlaylistByChannelNumber request,
        Parameters parameters,
        DateTimeOffset now)
    {
        string mode = string.IsNullOrWhiteSpace(request.Mode)
            ? string.Empty
            : $"&mode={request.Mode}";

        long index = GetIndexForChannel(parameters.Channel, parameters.PlayoutItem);
        double timeRemaining = Math.Abs((parameters.PlayoutItem.FinishOffset - now).TotalSeconds);
        return $@"#EXTM3U
#EXT-X-VERSION:3
#EXT-X-TARGETDURATION:10
#EXT-X-MEDIA-SEQUENCE:{index}
#EXT-X-DISCONTINUITY
#EXTINF:{timeRemaining:F2},
{request.Scheme}://{request.Host}/ffmpeg/stream/{request.ChannelNumber}?index={index}{mode}
".AsTask();
    }

    private Task<Validation<BaseError, Parameters>> Validate(
        TvContext dbContext,
        GetHlsPlaylistByChannelNumber request,
        DateTimeOffset now) =>
        ChannelMustExist(dbContext, request)
            .BindT(channel => PlayoutItemMustExist(dbContext, channel, now));

    private static Task<Validation<BaseError, Channel>> ChannelMustExist(
        TvContext dbContext,
        GetHlsPlaylistByChannelNumber request) =>
        dbContext.Channels
            .SelectOneAsync(c => c.Number, c => c.Number == request.ChannelNumber)
            .Map(o => o.ToValidation<BaseError>($"Channel number {request.ChannelNumber} does not exist."));

    private static Task<Validation<BaseError, Parameters>> PlayoutItemMustExist(
        TvContext dbContext,
        Channel channel,
        DateTimeOffset now) =>
        dbContext.PlayoutItems
            .ForChannelAndTime(channel.Id, now)
            .MapT(playoutItem => new Parameters(channel, playoutItem))
            .Map(o => o.ToValidation<BaseError>($"Unable to locate playout item for channel {channel.Number}"));

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

    private record Parameters(Channel Channel, PlayoutItem PlayoutItem);
}