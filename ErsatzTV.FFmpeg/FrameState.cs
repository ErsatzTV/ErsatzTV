﻿using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg;

public record FrameState(
    bool Realtime,
    bool InfiniteLoop,
    string VideoFormat,
    Option<string> VideoProfile,
    Option<string> VideoPreset,
    bool AllowBFrames,
    Option<IPixelFormat> PixelFormat,
    FrameSize ScaledSize,
    FrameSize PaddedSize,
    Option<FrameSize> CroppedSize,
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
