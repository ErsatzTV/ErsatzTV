using ErsatzTV.Core;

namespace ErsatzTV.Application.Streaming;

public record FFmpegProcessRequest(
    string ChannelNumber,
    string Mode,
    DateTimeOffset Now,
    bool StartAtZero,
    bool HlsRealtime,
    double PtsOffset) : IRequest<Either<BaseError, PlayoutItemProcessModel>>;
