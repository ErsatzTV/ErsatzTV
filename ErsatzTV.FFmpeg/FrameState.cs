using ErsatzTV.FFmpeg.Format;
using LanguageExt;

namespace ErsatzTV.FFmpeg;

public record FrameState(
    bool Realtime,
    bool InfiniteLoop,
    Option<TimeSpan> Start,
    Option<TimeSpan> Finish,
    string VideoFormat,
    Option<IPixelFormat> PixelFormat,
    FrameSize ScaledSize,
    FrameSize PaddedSize,
    Option<int> FrameRate,
    Option<int> VideoBitrate,
    Option<int> VideoBufferSize,
    Option<int> VideoTrackTimeScale,
    bool Deinterlaced,
    FrameDataLocation FrameDataLocation = FrameDataLocation.Unknown);