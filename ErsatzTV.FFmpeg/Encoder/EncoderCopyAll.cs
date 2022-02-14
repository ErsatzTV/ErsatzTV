namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderCopyAll : EncoderBase
{
    public override FrameState NextState(FrameState currentState) => currentState;
    public override string Name => "copy";
    public override StreamKind Kind => StreamKind.All;
    public override IList<string> OutputOptions => new List<string> { "-c", Name };
}
