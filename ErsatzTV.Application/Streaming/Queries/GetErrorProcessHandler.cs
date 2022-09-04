using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public class GetErrorProcessHandler : FFmpegProcessHandler<GetErrorProcess>
{
    private readonly IFFmpegProcessService _ffmpegProcessService;

    public GetErrorProcessHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IFFmpegProcessService ffmpegProcessService)
        : base(dbContextFactory) =>
        _ffmpegProcessService = ffmpegProcessService;

    protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        GetErrorProcess request,
        Channel channel,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken)
    {
        Command process = await _ffmpegProcessService.ForError(
            ffmpegPath,
            channel,
            request.MaybeDuration,
            request.ErrorMessage,
            request.HlsRealtime,
            request.PtsOffset,
            channel.FFmpegProfile.VaapiDriver,
            channel.FFmpegProfile.VaapiDevice,
            Optional(channel.FFmpegProfile.QsvExtraHardwareFrames));

        return new PlayoutItemProcessModel(process, request.MaybeDuration, request.Until);
    }
}
