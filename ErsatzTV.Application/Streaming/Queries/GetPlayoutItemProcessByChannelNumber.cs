using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Streaming;

public record GetPlayoutItemProcessByChannelNumber(
    string ChannelNumber,
    StreamingMode Mode,
    DateTimeOffset Now,
    bool StartAtZero,
    bool HlsRealtime,
    DateTimeOffset ChannelStart,
    TimeSpan PtsOffset,
    Option<int> TargetFramerate,
    bool IsTroubleshooting,
    Option<int> FFmpegProfileId) : FFmpegProcessRequest(
    ChannelNumber,
    Mode,
    Now,
    StartAtZero,
    HlsRealtime,
    ChannelStart,
    PtsOffset,
    FFmpegProfileId);
