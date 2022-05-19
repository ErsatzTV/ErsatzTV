﻿namespace ErsatzTV.FFmpeg.Decoder.Qsv;

public class DecoderMpeg2Qsv : DecoderBase
{
    public override string Name => "mpeg2_qsv";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
