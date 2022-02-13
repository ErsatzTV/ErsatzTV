using ErsatzTV.FFmpeg.Filter.Cuda;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableDeinterlaceFilters
{
    public static IPipelineFilterStep ForAcceleration(
        HardwareAccelerationMode accelMode,
        FrameState currentState) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new YadifCudaFilter(currentState),
            _ => new YadifFilter()
        };
}
