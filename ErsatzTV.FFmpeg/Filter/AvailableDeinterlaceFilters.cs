using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;
using ErsatzTV.FFmpeg.Filter.Vaapi;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableDeinterlaceFilters
{
    public static IPipelineFilterStep ForAcceleration(
        HardwareAccelerationMode accelMode,
        FrameState currentState) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new YadifCudaFilter(currentState),
            // deinterlace_qsv seems to create timestamp issues
            // HardwareAccelerationMode.Qsv => new DeinterlaceQsvFilter(currentState),
            HardwareAccelerationMode.Vaapi => new DeinterlaceVaapiFilter(currentState),
            _ => new YadifFilter(currentState)
        };
}
