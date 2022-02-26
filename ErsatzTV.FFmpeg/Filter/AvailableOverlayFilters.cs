using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableOverlayFilters
{
    public static IPipelineFilterStep ForAcceleration(
        HardwareAccelerationMode accelMode,
        WatermarkState watermarkState,
        FrameSize resolution) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new OverlayCudaFilter(watermarkState, resolution),
            HardwareAccelerationMode.Qsv => new OverlayQsvFilter(watermarkState, resolution),
            // HardwareAccelerationMode.Vaapi => new DeinterlaceVaapiFilter(),
            _ => new OverlayFilter(watermarkState, resolution)
        };
}
