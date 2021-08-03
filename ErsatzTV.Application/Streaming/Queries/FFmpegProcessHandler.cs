using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Streaming.Queries
{
    public abstract class FFmpegProcessHandler<T> : IRequestHandler<T, Either<BaseError, Process>>
        where T : FFmpegProcessRequest
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        protected FFmpegProcessHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<Either<BaseError, Process>> Handle(T request, CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Validation<BaseError, Tuple<Channel, string>> validation = await Validate(dbContext, request);
            return await validation.Match(
                tuple => GetProcess(dbContext, request, tuple.Item1, tuple.Item2),
                error => Task.FromResult<Either<BaseError, Process>>(error.Join()));
        }

        protected abstract Task<Either<BaseError, Process>> GetProcess(
            TvContext dbContext,
            T request,
            Channel channel,
            string ffmpegPath);

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
                            "ts" => StreamingMode.TransportStream,
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
}
