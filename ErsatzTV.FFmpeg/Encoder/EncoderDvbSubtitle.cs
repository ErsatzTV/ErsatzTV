namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderDvbSubtitle : EncoderBase
{
    public override string Name => "dvbsub";
    public override StreamKind Kind => StreamKind.Subtitle;
    public override FrameState NextState(FrameState currentState) => currentState;
}
