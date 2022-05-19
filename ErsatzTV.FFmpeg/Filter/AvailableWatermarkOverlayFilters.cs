using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableWatermarkOverlayFilters
{
    public static IPipelineFilterStep ForAcceleration(
        HardwareAccelerationMode accelMode,
        FrameState currentState,
        WatermarkState watermarkState,
        FrameSize resolution) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new OverlayWatermarkCudaFilter(currentState, watermarkState, resolution),
            HardwareAccelerationMode.Qsv => new OverlayWatermarkQsvFilter(currentState, watermarkState, resolution),
            _ => new OverlayWatermarkFilter(currentState, watermarkState, resolution)
        };
}
