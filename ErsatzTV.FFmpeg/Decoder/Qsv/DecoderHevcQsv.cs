namespace ErsatzTV.FFmpeg.Decoder.Qsv;

public class DecoderHevcQsv : DecoderBase
{
    public override string Name => "hevc_qsv";

    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
