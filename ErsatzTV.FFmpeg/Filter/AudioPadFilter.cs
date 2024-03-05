namespace ErsatzTV.FFmpeg.Filter;

public class AudioPadFilter : BaseFilter
{
    public override string Filter => "apad";

    public override FrameState NextState(FrameState currentState) => currentState;
}
