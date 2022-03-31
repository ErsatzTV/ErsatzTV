namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderCopySubtitle : EncoderBase
{
    public override FrameState NextState(FrameState currentState) => currentState;
    public override string Name => "webvtt";
    public override StreamKind Kind => StreamKind.Subtitle;
}
