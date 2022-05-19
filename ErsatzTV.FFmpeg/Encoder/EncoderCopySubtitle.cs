namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderCopySubtitle : EncoderBase
{
    public override string Name => "copy";
    public override StreamKind Kind => StreamKind.Subtitle;
    public override FrameState NextState(FrameState currentState) => currentState;
}
