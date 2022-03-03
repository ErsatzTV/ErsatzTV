using System.Diagnostics;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public class GetConcatProcessByChannelNumberHandler : FFmpegProcessHandler<GetConcatProcessByChannelNumber>
{
    private readonly IFFmpegProcessServiceFactory _ffmpegProcessServiceFactory;

    public GetConcatProcessByChannelNumberHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IFFmpegProcessServiceFactory ffmpegProcessServiceFactory)
        : base(dbContextFactory)
    {
        _ffmpegProcessServiceFactory = ffmpegProcessServiceFactory;
    }

    protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        GetConcatProcessByChannelNumber request,
        Channel channel,
        string ffmpegPath)
    {
        bool saveReports = await dbContext.ConfigElements
            .GetValue<bool>(ConfigElementKey.FFmpegSaveReports)
            .Map(result => result.IfNone(false));

        IFFmpegProcessService ffmpegProcessService = await _ffmpegProcessServiceFactory.GetService();
        Process process = ffmpegProcessService.ConcatChannel(
            ffmpegPath,
            saveReports,
            channel,
            request.Scheme,
            request.Host);

        return new PlayoutItemProcessModel(process, DateTimeOffset.MaxValue);
    }
}