namespace ErsatzTV.FFmpeg.Filter;

public class ResetPtsFilter(string fps) : BaseFilter
{
    public override string Filter => $"setpts=PTS-STARTPTS,fps={fps}";

    public override FrameState NextState(FrameState currentState) => currentState;
}
