using System;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Streaming;

public record FFmpegProcessRequest
(
    string ChannelNumber,
    string Mode,
    DateTimeOffset Now,
    bool StartAtZero,
    bool HlsRealtime,
    long PtsOffset) : IRequest<Either<BaseError, PlayoutItemProcessModel>>;