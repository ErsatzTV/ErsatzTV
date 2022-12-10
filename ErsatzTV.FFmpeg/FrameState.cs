using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg;

public record FrameState(
    bool Realtime,
    bool InfiniteLoop,
    string VideoFormat,
    string VideoProfile,
    Option<IPixelFormat> PixelFormat,
    FrameSize ScaledSize,
    FrameSize PaddedSize,
    bool IsAnamorphic,
    Option<int> FrameRate,
    Option<int> VideoBitrate,
    Option<int> VideoBufferSize,
    Option<int> VideoTrackTimeScale,
    bool Deinterlaced,
    FrameDataLocation FrameDataLocation = FrameDataLocation.Unknown)
{
    public string FFmpegAspectRatio => PaddedSize.Width == 640 ? "4/3" : "16/9";
    public int BitDepth => PixelFormat.Map(pf => pf.BitDepth).IfNone(8);
}
