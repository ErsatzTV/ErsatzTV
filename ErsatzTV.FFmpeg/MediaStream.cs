using ErsatzTV.FFmpeg.Format;
using LanguageExt;

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
    Option<string> FrameRate) : MediaStream(
    Index,
    Codec,
    StreamKind.Video);
