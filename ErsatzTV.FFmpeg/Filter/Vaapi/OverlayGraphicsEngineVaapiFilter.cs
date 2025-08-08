using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class OverlayGraphicsEngineVaapiFilter(FrameState currentState, IPixelFormat outputPixelFormat) : BaseFilter
{
    public override string Filter =>
        currentState.FrameDataLocation is FrameDataLocation.Hardware
            ? "overlay_vaapi"
            : $"overlay=format={(outputPixelFormat.BitDepth == 10 ? '1' : '0')}";

    public override FrameState NextState(FrameState currentState) => currentState;
}
