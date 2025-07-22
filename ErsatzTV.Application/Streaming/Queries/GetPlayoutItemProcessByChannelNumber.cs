namespace ErsatzTV.Application.Streaming;

public record GetPlayoutItemProcessByChannelNumber(
    string ChannelNumber,
    string Mode,
    DateTimeOffset Now,
    bool StartAtZero,
    bool HlsRealtime,
    double PtsOffset,
    Option<int> TargetFramerate) : FFmpegProcessRequest(
    ChannelNumber,
    Mode,
    Now,
    StartAtZero,
    HlsRealtime,
    PtsOffset);
