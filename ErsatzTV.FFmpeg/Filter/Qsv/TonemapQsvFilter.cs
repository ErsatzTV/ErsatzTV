namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class TonemapQsvFilter : BaseFilter
{
    public override string Filter => "vpp_qsv=tonemap=1";

    public override FrameState NextState(FrameState currentState) =>
        currentState with
        {
            FrameDataLocation = FrameDataLocation.Hardware
        };
}
