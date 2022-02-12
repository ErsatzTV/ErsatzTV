namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderMpeg2Cuvid : DecoderBase
{
    public override string Name => "mpeg2_cuvid";
    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
