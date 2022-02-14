using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;
using ErsatzTV.FFmpeg.Filter.Vaapi;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableScaleFilters
{
    public static IPipelineFilterStep ForAcceleration(
        HardwareAccelerationMode accelMode,
        FrameState currentState,
        FrameSize scaledSize,
        FrameSize paddedSize) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new ScaleCudaFilter(currentState, scaledSize, paddedSize),
            HardwareAccelerationMode.Qsv => new ScaleQsvFilter(currentState, scaledSize),
            HardwareAccelerationMode.Vaapi => new ScaleVaapiFilter(currentState, scaledSize, paddedSize),
            _ => new ScaleFilter(currentState, scaledSize, paddedSize)
        };
}
