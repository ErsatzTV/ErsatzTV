namespace ErsatzTV.FFmpeg.Filter;

public class OverlaySubtitleFilter : BaseFilter
{
    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter => "overlay";
}
