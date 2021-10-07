using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Streaming.Commands
{
    public class CleanUpFFmpegSessionsHandler : IRequestHandler<CleanUpFFmpegSessions, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IFFmpegWorkerRequest> _channel;

        public CleanUpFFmpegSessionsHandler(ChannelWriter<IFFmpegWorkerRequest> channel)
        {
            _channel = channel;
        }

        public async Task<Either<BaseError, Unit>>
            Handle(CleanUpFFmpegSessions request, CancellationToken cancellationToken)
        {
            await _channel.WriteAsync(request, cancellationToken);
            return Unit.Default;
        }
    }
}
