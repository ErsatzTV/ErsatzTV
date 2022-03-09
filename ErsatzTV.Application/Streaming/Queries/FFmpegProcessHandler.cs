using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public abstract class FFmpegProcessHandler<T> : IRequestHandler<T, Either<BaseError, PlayoutItemProcessModel>>
    where T : FFmpegProcessRequest
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    protected FFmpegProcessHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, PlayoutItemProcessModel>> Handle(T request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Tuple<Channel, string>> validation = await Validate(dbContext, request);
        return await validation.Match(
            tuple => GetProcess(dbContext, request, tuple.Item1, tuple.Item2, cancellationToken),
            error => Task.FromResult<Either<BaseError, PlayoutItemProcessModel>>(error.Join()));
    }

    protected abstract Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        T request,
        Channel channel,
        string ffmpegPath,
        CancellationToken cancellationToken);

    private static async Task<Validation<BaseError, Tuple<Channel, string>>> Validate(
        TvContext dbContext,
        T request) =>
        (await ChannelMustExist(dbContext, request), await FFmpegPathMustExist(dbContext))
        .Apply((channel, ffmpegPath) => Tuple(channel, ffmpegPath));

    private static Task<Validation<BaseError, Channel>> ChannelMustExist(TvContext dbContext, T request) =>
        dbContext.Channels
            .Include(c => c.FFmpegProfile)
            .ThenInclude(p => p.Resolution)
            .Include(c => c.Artwork)
            .Include(c => c.Watermark)
            .SelectOneAsync(c => c.Number, c => c.Number == request.ChannelNumber)
            .MapT(
                channel =>
                {
                    channel.StreamingMode = request.Mode.ToLowerInvariant() switch
                    {
                        "hls-direct" => StreamingMode.HttpLiveStreamingDirect,
                        "segmenter" => StreamingMode.HttpLiveStreamingSegmenter,
                        "ts" => StreamingMode.TransportStreamHybrid,
                        "ts-legacy" => StreamingMode.TransportStream,
                        _ => channel.StreamingMode
                    };

                    return channel;
                })
            .Map(o => o.ToValidation<BaseError>($"Channel number {request.ChannelNumber} does not exist."));

    private static Task<Validation<BaseError, string>> FFmpegPathMustExist(TvContext dbContext) =>
        dbContext.ConfigElements.GetValue<string>(ConfigElementKey.FFmpegPath)
            .FilterT(File.Exists)
            .Map(maybePath => maybePath.ToValidation<BaseError>("FFmpeg path does not exist on filesystem"));
}