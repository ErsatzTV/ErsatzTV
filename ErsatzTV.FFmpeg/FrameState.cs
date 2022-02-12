using ErsatzTV.FFmpeg.Format;
using LanguageExt;

namespace ErsatzTV.FFmpeg;

public record FrameState(
    string VideoFormat,
    IPixelFormat PixelFormat,
    Option<int> VideoBitrate,
    Option<int> VideoBufferSize,
    string AudioFormat,
    int AudioChannels,
    Option<int> AudioBitrate,
    Option<int> AudioBufferSize,
    FrameDataLocation FrameDataLocation = FrameDataLocation.Unknown);
