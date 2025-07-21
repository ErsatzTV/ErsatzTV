namespace ErsatzTV.FFmpeg.Filter;

public class AudioSetPtsFilter : BaseFilter
{
    public override string Filter => "asetpts=PTS-STARTPTS";

    public override FrameState NextState(FrameState currentState) => currentState;
}
