﻿using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Nvenc;

public class EncoderHevcNvenc : EncoderBase
{
    public override string Name => "hevc_nvenc";
    public override StreamKind Kind => StreamKind.Video;
    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc
    };
}
