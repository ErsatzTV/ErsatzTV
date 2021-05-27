using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Runtime;
using LanguageExt;

namespace ErsatzTV.Application.Streaming.Queries
{
    public class GetConcatProcessByChannelNumberHandler : FFmpegProcessHandler<GetConcatProcessByChannelNumber>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly FFmpegProcessService _ffmpegProcessService;
        private readonly IRuntimeInfo _runtimeInfo;

        public GetConcatProcessByChannelNumberHandler(
            IChannelRepository channelRepository,
            IConfigElementRepository configElementRepository,
            FFmpegProcessService ffmpegProcessService,
            IRuntimeInfo runtimeInfo)
            : base(channelRepository, configElementRepository)
        {
            _configElementRepository = configElementRepository;
            _ffmpegProcessService = ffmpegProcessService;
            _runtimeInfo = runtimeInfo;
        }

        protected override async Task<Either<BaseError, Process>> GetProcess(
            GetConcatProcessByChannelNumber request,
            Channel channel,
            string ffmpegPath)
        {
            bool saveReports = !_runtimeInfo.IsOSPlatform(OSPlatform.Windows) && await _configElementRepository
                .GetValue<bool>(ConfigElementKey.FFmpegSaveReports)
                .Map(result => result.IfNone(false));

            return _ffmpegProcessService.ConcatChannel(
                ffmpegPath,
                saveReports,
                channel,
                request.Scheme,
                request.Host);
        }
    }
}
