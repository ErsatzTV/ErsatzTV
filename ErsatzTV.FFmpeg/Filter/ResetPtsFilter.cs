namespace ErsatzTV.FFmpeg.Filter;

public class ResetPtsFilter : BaseFilter
{
    public override string Filter => "setpts=PTS-STARTPTS";

    public override FrameState NextState(FrameState currentState) => currentState;
}
