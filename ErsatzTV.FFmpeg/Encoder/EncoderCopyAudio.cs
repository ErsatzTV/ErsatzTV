namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderCopyAudio : EncoderBase
{
    public override FrameState NextState(FrameState currentState) => currentState;
    public override string Name => "copy";
    public override StreamKind Kind => StreamKind.Audio;
}
