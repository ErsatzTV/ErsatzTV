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
        Validation<BaseError, Tuple<Channel, string, string>> validation = await Validate(
            dbContext,
            request,
            cancellationToken);
        return await validation.Match(
            tuple => GetProcess(
                dbContext,
                request with { Now = request.Now - (tuple.Item1.PlayoutOffset ?? TimeSpan.Zero) },
                tuple.Item1,
                tuple.Item2,
                tuple.Item3,
                cancellationToken),
            error => Task.FromResult<Either<BaseError, PlayoutItemProcessModel>>(error.Join()));
    }

    protected abstract Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        T request,
        Channel channel,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken);

    private static async Task<Validation<BaseError, Tuple<Channel, string, string>>> Validate(
        TvContext dbContext,
        T request,
        CancellationToken cancellationToken) =>
        (await ChannelMustExist(dbContext, request, cancellationToken),
            await FFmpegPathMustExist(dbContext, cancellationToken),
            await FFprobePathMustExist(dbContext, cancellationToken))
        .Apply((channel, ffmpegPath, ffprobePath) => Tuple(channel, ffmpegPath, ffprobePath));

    private static Task<Validation<BaseError, Channel>> ChannelMustExist(
        TvContext dbContext,
        T request,
        CancellationToken cancellationToken) =>
        dbContext.Channels
            .Include(c => c.FFmpegProfile)
            .ThenInclude(p => p.Resolution)
            .Include(c => c.Artwork)
            .Include(c => c.Watermark)
            .SelectOneAsync(c => c.Number, c => c.Number == request.ChannelNumber, cancellationToken)
            .MapT(channel =>
            {
                channel.StreamingMode = request.Mode.ToLowerInvariant() switch
                {
                    "hls-direct" => StreamingMode.HttpLiveStreamingDirect,
                    "segmenter" => StreamingMode.HttpLiveStreamingSegmenter,
                    "segmenter-v2" => StreamingMode.HttpLiveStreamingSegmenterV2,
                    "ts" => StreamingMode.TransportStreamHybrid,
                    "ts-legacy" => StreamingMode.TransportStream,
                    _ => channel.StreamingMode
                };

                return channel;
            })
            .Map(o => o.ToValidation<BaseError>($"Channel number {request.ChannelNumber} does not exist."));

    private static Task<Validation<BaseError, string>> FFmpegPathMustExist(
        TvContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.ConfigElements.GetValue<string>(ConfigElementKey.FFmpegPath, cancellationToken)
            .FilterT(File.Exists)
            .Map(maybePath => maybePath.ToValidation<BaseError>("FFmpeg path does not exist on filesystem"));

    private static Task<Validation<BaseError, string>> FFprobePathMustExist(
        TvContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.ConfigElements.GetValue<string>(ConfigElementKey.FFprobePath, cancellationToken)
            .FilterT(File.Exists)
            .Map(maybePath => maybePath.ToValidation<BaseError>("FFprobe path does not exist on filesystem"));
}
