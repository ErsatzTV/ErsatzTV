namespace ErsatzTV.FFmpeg.Filter;

public class LoopFilter : BaseFilter
{
    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter => "loop=-1:1";
}
