namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderDvdSubtitle : EncoderBase
{
    public override string Name => "dvdsub";
    public override StreamKind Kind => StreamKind.Subtitle;
    public override FrameState NextState(FrameState currentState) => currentState;
}
