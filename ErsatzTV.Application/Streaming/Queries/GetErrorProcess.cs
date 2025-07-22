namespace ErsatzTV.Application.Streaming;

public record GetErrorProcess(
    string ChannelNumber,
    string Mode,
    bool HlsRealtime,
    double PtsOffset,
    Option<TimeSpan> MaybeDuration,
    DateTimeOffset Until,
    string ErrorMessage) : FFmpegProcessRequest(
    ChannelNumber,
    Mode,
    DateTimeOffset.Now,
    true,
    HlsRealtime,
    PtsOffset);
