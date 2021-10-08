using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Streaming.Commands
{
    public record CleanUpFFmpegSessions : IRequest<Either<BaseError, Unit>>, IFFmpegWorkerRequest;
}
