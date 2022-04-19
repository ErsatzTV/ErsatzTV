namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderCopyVideo : EncoderBase
{
    public override string Name => "copy";
    public override StreamKind Kind => StreamKind.Video;
    public override FrameState NextState(FrameState currentState) => currentState;
}
