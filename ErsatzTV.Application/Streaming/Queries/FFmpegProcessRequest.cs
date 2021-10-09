using System;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Streaming.Queries
{
    public record FFmpegProcessRequest
    (
        string ChannelNumber,
        string Mode,
        DateTimeOffset Now,
        bool StartAtZero) : IRequest<Either<BaseError, PlayoutItemProcessModel>>;
}
