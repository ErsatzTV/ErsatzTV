using ErsatzTV.FFmpeg.Filter.Cuda;

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
            _ => new ScaleFilter(scaledSize, paddedSize)
        };
}
