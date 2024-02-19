namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderPcmS16Le : EncoderBase
{
    public override string Name => "pcm_s16le";

    public override StreamKind Kind => StreamKind.Audio;
}
