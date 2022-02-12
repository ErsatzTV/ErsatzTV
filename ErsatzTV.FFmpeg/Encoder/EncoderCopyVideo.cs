﻿namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderCopyVideo : EncoderBase
{
    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public override FrameState NextState(FrameState currentState) => currentState;
    public override string Name => "copy";
    public override StreamKind Kind => StreamKind.Video;
}
