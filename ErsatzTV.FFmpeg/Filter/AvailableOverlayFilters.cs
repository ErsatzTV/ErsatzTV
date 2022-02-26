using ErsatzTV.FFmpeg.Filter.Cuda;
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
            // HardwareAccelerationMode.Qsv => new DeinterlaceQsvFilter(),
            // HardwareAccelerationMode.Vaapi => new DeinterlaceVaapiFilter(),
            _ => new OverlayFilter(watermarkState, resolution)
        };
}
