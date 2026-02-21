using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public class GetSlugProcessByChannelNumberHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IFFmpegProcessService ffmpegProcessService)
    : FFmpegProcessHandler<GetSlugProcessByChannelNumber>(dbContextFactory)
{
    protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        GetSlugProcessByChannelNumber request,
        Channel channel,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken)
    {
        var duration = TimeSpan.FromSeconds(await request.SlugSeconds.IfNoneAsync(0));
        if (duration <= TimeSpan.Zero)
        {
            return BaseError.New("Slug seconds must be non-zero");
        }

        DateTimeOffset finish = request.Now.Add(duration);

        var playoutItemResult = await ffmpegProcessService.Slug(
            ffmpegPath,
            channel,
            request.Now,
            duration,
            request.HlsRealtime,
            request.PtsOffset,
            cancellationToken);

        var result = new PlayoutItemProcessModel(
            playoutItemResult,
            Option<GraphicsEngineContext>.None,
            duration,
            finish,
            isComplete: true,
            request.Now.ToUnixTimeSeconds(),
            Option<int>.None,
            Optional(channel.PlayoutOffset),
            !request.HlsRealtime);

        return Right<BaseError, PlayoutItemProcessModel>(result);
    }
}
