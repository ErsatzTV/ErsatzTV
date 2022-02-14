namespace ErsatzTV.FFmpeg.Filter;

public class SetSarFilter : BaseFilter
{
    public override string Filter => "setsar=1";
    public override FrameState NextState(FrameState currentState) => currentState;
}
