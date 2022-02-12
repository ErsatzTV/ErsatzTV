using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg;

public record FrameState(
    string VideoFormat,
    IPixelFormat PixelFormat,
    string AudioFormat,
    FrameDataLocation FrameDataLocation = FrameDataLocation.Unknown);
