﻿namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderMpeg4Cuvid : DecoderBase
{
    public override string Name => "mpeg4_cuvid";

    public override IList<string> InputOptions
    {
        get
        {
            IList<string> result =  base.InputOptions;

            result.Add("-hwaccel_output_format");
            result.Add("cuda");

            return result;
        }
    }

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
