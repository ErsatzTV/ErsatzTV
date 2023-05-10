namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class OverlaySubtitleQsvFilter : BaseFilter
{
    public override string Filter =>
        "overlay_qsv=eof_action=endall:shortest=1:repeatlast=0:x=(W-w)/2:y=(H-h)/2";

    public override FrameState NextState(FrameState currentState) => currentState;
}
