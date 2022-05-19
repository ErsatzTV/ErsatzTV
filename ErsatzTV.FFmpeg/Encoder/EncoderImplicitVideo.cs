namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderImplicitVideo : EncoderBase
{
    public override string Name => string.Empty;
    public override StreamKind Kind => StreamKind.Video;
    public override IList<string> OutputOptions => Array.Empty<string>();
    public override FrameState NextState(FrameState currentState) => currentState;
}
