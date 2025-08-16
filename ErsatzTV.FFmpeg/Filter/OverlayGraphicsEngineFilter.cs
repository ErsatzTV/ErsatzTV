using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class OverlayGraphicsEngineFilter(IPixelFormat outputPixelFormat) : BaseFilter
{
    public override string Filter => $"overlay=format={(outputPixelFormat.BitDepth == 10 ? '1' : '0')}:alpha=premultiplied";

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Software };
}
