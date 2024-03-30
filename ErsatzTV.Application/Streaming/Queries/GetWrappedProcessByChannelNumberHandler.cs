﻿using CliWrap;
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
        : base(dbContextFactory) =>
        _ffmpegProcessService = ffmpegProcessService;

    protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        GetWrappedProcessByChannelNumber request,
        Channel channel,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken)
    {
        bool saveReports = await dbContext.ConfigElements
            .GetValue<bool>(ConfigElementKey.FFmpegSaveReports)
            .Map(result => result.IfNone(false));

        Command process = await _ffmpegProcessService.WrapSegmenter(
            ffmpegPath,
            saveReports,
            channel,
            request.Scheme,
            request.Host,
            request.AccessToken);

        return new PlayoutItemProcessModel(process, Option<TimeSpan>.None, DateTimeOffset.MaxValue, true);
    }
}
