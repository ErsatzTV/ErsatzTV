namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class QsvResetPtsFilter : BaseFilter
{
    public override string Filter => "setpts=PTS-STARTPTS";

    public override FrameState NextState(FrameState currentState) => currentState;
}
