namespace ErsatzTV.FFmpeg.Filter;

public class RealtimeFilter : BaseFilter
{
    public override string Filter => "realtime";
    public override FrameState NextState(FrameState currentState) => currentState with { Realtime = true };
}
