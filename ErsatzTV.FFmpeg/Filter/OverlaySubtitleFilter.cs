namespace ErsatzTV.FFmpeg.Filter;

public class OverlaySubtitleFilter : BaseFilter
{
    public override string Filter => "overlay";
    public override FrameState NextState(FrameState currentState) => currentState;
}
