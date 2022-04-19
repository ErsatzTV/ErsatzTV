namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderCopyAudio : EncoderBase
{
    public override string Name => "copy";
    public override StreamKind Kind => StreamKind.Audio;
    public override FrameState NextState(FrameState currentState) => currentState;
}
