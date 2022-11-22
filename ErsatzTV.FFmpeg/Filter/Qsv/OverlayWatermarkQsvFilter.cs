using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class OverlayWatermarkQsvFilter : OverlayWatermarkFilter
{
    public OverlayWatermarkQsvFilter(
        WatermarkState watermarkState,
        FrameSize resolution,
        FrameSize squarePixelFrameSize,
        ILogger logger) : base(
        watermarkState,
        resolution,
        squarePixelFrameSize,
        logger)
    {
    }

    public override string Filter => $"overlay_qsv={Position}";

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Hardware };
}
