using System.Diagnostics;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public class GetConcatProcessByChannelNumberHandler : FFmpegProcessHandler<GetConcatProcessByChannelNumber>
{
    private readonly IFFmpegProcessService _ffmpegProcessService;

    public GetConcatProcessByChannelNumberHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IFFmpegProcessService ffmpegProcessService)
        : base(dbContextFactory)
    {
        _ffmpegProcessService = ffmpegProcessService;
    }

    protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        GetConcatProcessByChannelNumber request,
        Channel channel,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        bool saveReports = await dbContext.ConfigElements
            .GetValue<bool>(ConfigElementKey.FFmpegSaveReports)
            .Map(result => result.IfNone(false));

        Process process = _ffmpegProcessService.ConcatChannel(
            ffmpegPath,
            saveReports,
            channel,
            request.Scheme,
            request.Host);

        return new PlayoutItemProcessModel(process, DateTimeOffset.MaxValue);
    }
}