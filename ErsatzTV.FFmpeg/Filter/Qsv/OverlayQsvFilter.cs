using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class OverlayQsvFilter : OverlayFilter
{
    public OverlayQsvFilter(FrameState currentState, WatermarkState watermarkState, FrameSize resolution) : base(
        currentState,
        watermarkState,
        resolution)
    {
    }

    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter => $"overlay_qsv={Position}";
}
