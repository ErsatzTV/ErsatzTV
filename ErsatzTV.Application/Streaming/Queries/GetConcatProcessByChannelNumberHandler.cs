using System.Diagnostics;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Streaming.Queries
{
    public class GetConcatProcessByChannelNumberHandler : FFmpegProcessHandler<GetConcatProcessByChannelNumber>
    {
        private readonly FFmpegProcessService _ffmpegProcessService;

        public GetConcatProcessByChannelNumberHandler(
            IChannelRepository channelRepository,
            IConfigElementRepository configElementRepository,
            FFmpegProcessService ffmpegProcessService)
            : base(channelRepository, configElementRepository) =>
            _ffmpegProcessService = ffmpegProcessService;

        protected override Task<Either<BaseError, Process>> GetProcess(
            GetConcatProcessByChannelNumber request,
            Channel channel,
            string ffmpegPath) =>
            Right<BaseError, Process>(
                _ffmpegProcessService.ConcatChannel(
                    ffmpegPath,
                    channel,
                    request.Scheme,
                    request.Host)).AsTask();
    }
}
