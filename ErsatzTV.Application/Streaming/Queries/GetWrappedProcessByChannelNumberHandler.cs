using System.Diagnostics;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public class GetWrappedProcessByChannelNumberHandler : FFmpegProcessHandler<GetWrappedProcessByChannelNumber>
{
    private readonly IFFmpegProcessService _ffmpegProcessService;

    public GetWrappedProcessByChannelNumberHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IFFmpegProcessService ffmpegProcessService)
        : base(dbContextFactory)
    {
        _ffmpegProcessService = ffmpegProcessService;
    }

    protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        GetWrappedProcessByChannelNumber request,
        Channel channel,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        bool saveReports = await dbContext.ConfigElements
            .GetValue<bool>(ConfigElementKey.FFmpegSaveReports)
            .Map(result => result.IfNone(false));

        Process process = _ffmpegProcessService.WrapSegmenter(
            ffmpegPath,
            saveReports,
            channel,
            request.Scheme,
            request.Host);

        return new PlayoutItemProcessModel(process, Option<TimeSpan>.None, DateTimeOffset.MaxValue);
    }
}