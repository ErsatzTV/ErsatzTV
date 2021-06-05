using System.Diagnostics;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Streaming.Queries
{
    public record FFmpegProcessRequest(string ChannelNumber, string Mode) : IRequest<Either<BaseError, Process>>;
}
