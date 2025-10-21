using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Streaming;

public record GetErrorProcess(
    string ChannelNumber,
    StreamingMode Mode,
    bool HlsRealtime,
    TimeSpan PtsOffset,
    Option<TimeSpan> MaybeDuration,
    DateTimeOffset Until,
    string ErrorMessage) : FFmpegProcessRequest(
    ChannelNumber,
    Mode,
    DateTimeOffset.Now,
    true,
    HlsRealtime,
    DateTimeOffset.Now, // unused
    PtsOffset);
