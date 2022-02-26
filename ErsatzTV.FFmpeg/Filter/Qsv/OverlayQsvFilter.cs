using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class OverlayQsvFilter : OverlayFilter
{
    public OverlayQsvFilter(WatermarkState watermarkState, FrameSize resolution) : base(watermarkState, resolution)
    {
    }

    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter => $"overlay_qsv={Position}";
}
