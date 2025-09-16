using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class OverlayGraphicsEngineFilter(IPixelFormat outputPixelFormat) : BaseFilter
{
    public override string Filter
    {
        get
        {
            string extraFormat = outputPixelFormat.BitDepth == 10 ? ",format=p010le" : string.Empty;
            return $"overlay=format={(outputPixelFormat.BitDepth == 10 ? "yuv420p10" : "0")}{extraFormat}";
        }
    }

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Software };
}
