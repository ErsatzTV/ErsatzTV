using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public class GetConcatSegmenterProcessByChannelNumberHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IFFmpegProcessService ffmpegProcessService)
    : FFmpegProcessHandler<GetConcatSegmenterProcessByChannelNumber>(dbContextFactory)
{
    protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        GetConcatSegmenterProcessByChannelNumber request,
        Channel channel,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken)
    {
        bool saveReports = await dbContext.ConfigElements
            .GetValue<bool>(ConfigElementKey.FFmpegSaveReports)
            .Map(result => result.IfNone(false));

        Command process = await ffmpegProcessService.ConcatSegmenterChannel(
            ffmpegPath,
            saveReports,
            channel,
            request.Scheme,
            request.Host);

        return new PlayoutItemProcessModel(process, Option<TimeSpan>.None, DateTimeOffset.MaxValue, true);
    }
}
