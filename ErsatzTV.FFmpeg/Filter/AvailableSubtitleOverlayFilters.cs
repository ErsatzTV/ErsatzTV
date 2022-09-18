using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableSubtitleOverlayFilters
{
    public static IPipelineFilterStep ForAcceleration(HardwareAccelerationMode accelMode) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new OverlaySubtitleCudaFilter(),
            HardwareAccelerationMode.Qsv => new OverlaySubtitleQsvFilter(),
            _ => new OverlaySubtitleFilter()
        };
}
