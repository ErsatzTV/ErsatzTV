namespace ErsatzTV.FFmpeg.Filter;

public class OverlaySubtitleFilter : BaseFilter
{
    public override string Filter => "overlay=x=(W-w)/2:y=(H-h)/2";
    public override FrameState NextState(FrameState currentState) => currentState;
}
