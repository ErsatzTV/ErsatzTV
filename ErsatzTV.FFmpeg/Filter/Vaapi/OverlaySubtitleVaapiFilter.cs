namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class OverlaySubtitleVaapiFilter : BaseFilter
{
    public override string Filter => "overlay_vaapi";
    public override FrameState NextState(FrameState currentState) => currentState;
}
