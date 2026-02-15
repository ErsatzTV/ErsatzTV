using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg;

namespace ErsatzTV.Application.Streaming;

public record GetSlugProcessByChannelNumber(
    string ChannelNumber,
    StreamingMode Mode,
    DateTimeOffset Now,
    bool HlsRealtime,
    DateTimeOffset ChannelStart,
    TimeSpan PtsOffset,
    Option<FrameRate> TargetFramerate,
    Option<double> SlugSeconds) : FFmpegProcessRequest(
    ChannelNumber,
    Mode,
    Now,
    StartAtZero: true,
    HlsRealtime,
    ChannelStart,
    PtsOffset,
    FFmpegProfileId: Option<int>.None);
