using System.Diagnostics;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.Streaming.Queries
{
    public class GetConcatProcessByChannelNumberHandler : FFmpegProcessHandler<GetConcatProcessByChannelNumber>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly FFmpegProcessService _ffmpegProcessService;

        public GetConcatProcessByChannelNumberHandler(
            IChannelRepository channelRepository,
            IConfigElementRepository configElementRepository,
            FFmpegProcessService ffmpegProcessService)
            : base(channelRepository, configElementRepository)
        {
            _configElementRepository = configElementRepository;
            _ffmpegProcessService = ffmpegProcessService;
        }

        protected override async Task<Either<BaseError, Process>> GetProcess(
            GetConcatProcessByChannelNumber request,
            Channel channel,
            string ffmpegPath)
        {
            bool saveReports = await _configElementRepository.GetValue<bool>(ConfigElementKey.FFmpegSaveReports)
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
