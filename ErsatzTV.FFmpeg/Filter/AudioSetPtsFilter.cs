namespace ErsatzTV.FFmpeg.Filter;

public class AudioSetPtsFilter : BaseFilter
{
    public override string Filter => "asetpts=N/SR/TB";

    public override FrameState NextState(FrameState currentState) => currentState;
}
