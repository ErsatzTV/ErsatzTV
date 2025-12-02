using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg;

namespace ErsatzTV.Application.Streaming;

public record GetPlayoutItemProcessByChannelNumber(
    string ChannelNumber,
    StreamingMode Mode,
    DateTimeOffset Now,
    bool StartAtZero,
    bool HlsRealtime,
    DateTimeOffset ChannelStart,
    TimeSpan PtsOffset,
    Option<FrameRate> TargetFramerate,
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
