using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class OverlayWatermarkQsvFilter : OverlayWatermarkFilter
{
    public OverlayWatermarkQsvFilter(
        FrameState currentState,
        WatermarkState watermarkState,
        FrameSize resolution) : base(
        currentState,
        watermarkState,
        resolution)
    {
    }

    public override string Filter => $"overlay_qsv={Position}";

    public override FrameState NextState(FrameState currentState) => currentState;
}
