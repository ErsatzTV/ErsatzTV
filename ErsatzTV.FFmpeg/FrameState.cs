using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg;

public record FrameState(
    bool Realtime,
    bool InfiniteLoop,
    string VideoFormat,
    Option<IPixelFormat> PixelFormat,
    FrameSize ScaledSize,
    FrameSize PaddedSize,
    string DisplayAspectRatio,
    Option<int> FrameRate,
    Option<int> VideoBitrate,
    Option<int> VideoBufferSize,
    Option<int> VideoTrackTimeScale,
    bool Deinterlaced,
    FrameDataLocation FrameDataLocation = FrameDataLocation.Unknown);
