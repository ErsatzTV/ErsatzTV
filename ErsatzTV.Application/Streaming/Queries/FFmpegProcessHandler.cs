using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Streaming.Queries
{
    public abstract class FFmpegProcessHandler<T> : IRequestHandler<T, Either<BaseError, Process>>
        where T : FFmpegProcessRequest
    {
        private readonly IChannelRepository _channelRepository;
        private readonly IConfigElementRepository _configElementRepository;

        protected FFmpegProcessHandler(
            IChannelRepository channelRepository,
            IConfigElementRepository configElementRepository)
        {
            _channelRepository = channelRepository;
            _configElementRepository = configElementRepository;
        }

        public Task<Either<BaseError, Process>> Handle(T request, CancellationToken cancellationToken) =>
            Validate(request)
                .Map(v => v.ToEither<Tuple<Channel, string>>())
                .BindT(tuple => GetProcess(request, tuple.Item1, tuple.Item2));

        protected abstract Task<Either<BaseError, Process>> GetProcess(T request, Channel channel, string ffmpegPath);

        private async Task<Validation<BaseError, Tuple<Channel, string>>> Validate(T request) =>
            (await ChannelMustExist(request), await FFmpegPathMustExist())
            .Apply((channel, ffmpegPath) => Tuple(channel, ffmpegPath));

        private async Task<Validation<BaseError, Channel>> ChannelMustExist(T request) =>
            (await _channelRepository.GetByNumber(request.ChannelNumber))
            .ToValidation<BaseError>($"Channel number {request.ChannelNumber} does not exist.");

        private Task<Validation<BaseError, string>> FFmpegPathMustExist() =>
            _configElementRepository.GetValue<string>(ConfigElementKey.FFmpegPath)
                .FilterT(File.Exists)
                .Map(maybePath => maybePath.ToValidation<BaseError>("FFmpeg path does not exist on filesystem"));
    }
}
