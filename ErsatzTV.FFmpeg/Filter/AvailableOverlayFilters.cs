using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableOverlayFilters
{
    public static IPipelineFilterStep ForAcceleration(
        HardwareAccelerationMode accelMode,
        FrameState currentState,
        WatermarkState watermarkState,
        FrameSize resolution) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new OverlayCudaFilter(currentState, watermarkState, resolution),
            HardwareAccelerationMode.Qsv => new OverlayQsvFilter(currentState, watermarkState, resolution),
            _ => new OverlayFilter(currentState, watermarkState, resolution)
        };
}
