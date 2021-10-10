using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Runtime;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming.Queries
{
    public class GetConcatProcessByChannelNumberHandler : FFmpegProcessHandler<GetConcatProcessByChannelNumber>
    {
        private readonly FFmpegProcessService _ffmpegProcessService;
        private readonly IRuntimeInfo _runtimeInfo;

        public GetConcatProcessByChannelNumberHandler(
            IDbContextFactory<TvContext> dbContextFactory,
            FFmpegProcessService ffmpegProcessService,
            IRuntimeInfo runtimeInfo)
            : base(dbContextFactory)
        {
            _ffmpegProcessService = ffmpegProcessService;
            _runtimeInfo = runtimeInfo;
        }

        protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
            TvContext dbContext,
            GetConcatProcessByChannelNumber request,
            Channel channel,
            string ffmpegPath)
        {
            bool saveReports = !_runtimeInfo.IsOSPlatform(OSPlatform.Windows) && await dbContext.ConfigElements
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
}
