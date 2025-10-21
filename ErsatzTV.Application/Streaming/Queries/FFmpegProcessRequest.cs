﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Streaming;

public record FFmpegProcessRequest(
    string ChannelNumber,
    StreamingMode Mode,
    DateTimeOffset Now,
    bool StartAtZero,
    bool HlsRealtime,
    DateTimeOffset ChannelStartTime,
    TimeSpan PtsOffset) : IRequest<Either<BaseError, PlayoutItemProcessModel>>;
