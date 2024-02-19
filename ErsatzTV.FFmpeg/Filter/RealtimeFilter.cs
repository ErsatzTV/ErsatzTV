namespace ErsatzTV.FFmpeg.Filter;

public class RealtimeFilter : BaseFilter
{
    public override FrameState NextState(FrameState currentState) => currentState with { Realtime = true };

    public override string Filter => "realtime";
}
