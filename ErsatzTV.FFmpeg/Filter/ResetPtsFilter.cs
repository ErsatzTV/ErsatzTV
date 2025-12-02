namespace ErsatzTV.FFmpeg.Filter;

public class ResetPtsFilter(FrameRate frameRate) : BaseFilter
{
    public override string Filter => $"setpts=PTS-STARTPTS,fps={frameRate.RFrameRate}";

    public override FrameState NextState(FrameState currentState) => currentState with { FrameRate = frameRate };
}
