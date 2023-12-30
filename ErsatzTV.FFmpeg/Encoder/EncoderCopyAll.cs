namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderCopyAll : EncoderBase
{
    public override string Name => "copy";
    public override StreamKind Kind => StreamKind.All;
    public override string[] OutputOptions => new[] { "-c", Name };
    public override FrameState NextState(FrameState currentState) => currentState;
}
