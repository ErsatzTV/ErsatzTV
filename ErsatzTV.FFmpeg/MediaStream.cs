using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg;

public record MediaStream(int Index, string Codec, StreamKind Kind);

public record AudioStream(int Index, string Codec, int Channels) : MediaStream(
    Index,
    Codec,
    StreamKind.Audio);

public record VideoStream(
    int Index,
    string Codec,
    Option<IPixelFormat> PixelFormat,
    FrameSize FrameSize,
    string DisplayAspectRatio,
    Option<string> FrameRate,
    bool StillImage) : MediaStream(
    Index,
    Codec,
    StreamKind.Video);
