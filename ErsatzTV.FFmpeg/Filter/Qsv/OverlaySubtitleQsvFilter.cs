namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class OverlaySubtitleQsvFilter : BaseFilter
{
    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter => "overlay_qsv";
}
