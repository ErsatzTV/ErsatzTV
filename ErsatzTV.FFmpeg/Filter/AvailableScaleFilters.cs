using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;

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
            _ => new ScaleFilter(currentState, scaledSize, paddedSize)
        };
}
