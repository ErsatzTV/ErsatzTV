using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public class GetErrorProcessHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IFFmpegProcessService ffmpegProcessService)
    : FFmpegProcessHandler<GetErrorProcess>(dbContextFactory)
{
    protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        GetErrorProcess request,
        Channel channel,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        Command process = await ffmpegProcessService.ForError(
            ffmpegPath,
            channel,
            now,
            request.MaybeDuration,
            request.ErrorMessage,
            request.HlsRealtime,
            request.PtsOffset,
            channel.FFmpegProfile.VaapiDisplay,
            channel.FFmpegProfile.VaapiDriver,
            channel.FFmpegProfile.VaapiDevice,
            Optional(channel.FFmpegProfile.QsvExtraHardwareFrames));

        return new PlayoutItemProcessModel(
            process,
            Option<GraphicsEngineContext>.None,
            request.MaybeDuration,
            request.Until,
            true,
            now.ToUnixTimeSeconds());
    }
}
