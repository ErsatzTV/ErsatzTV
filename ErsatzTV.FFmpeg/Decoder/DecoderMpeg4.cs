﻿namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderMpeg4 : DecoderBase
{
    public override string Name => "mpeg4";
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
