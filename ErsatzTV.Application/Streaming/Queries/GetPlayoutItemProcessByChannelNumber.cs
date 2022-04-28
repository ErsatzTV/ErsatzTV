namespace ErsatzTV.Application.Streaming;

public record GetPlayoutItemProcessByChannelNumber(
    string ChannelNumber,
    string Mode,
    DateTimeOffset Now,
    bool StartAtZero,
    bool HlsRealtime,
    long PtsOffset,
    Option<int> TargetFramerate) : FFmpegProcessRequest(
    ChannelNumber,
    Mode,
    Now,
    StartAtZero,
    HlsRealtime,
    PtsOffset);
