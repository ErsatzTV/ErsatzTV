using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;
using ErsatzTV.FFmpeg.Filter.Vaapi;
using ErsatzTV.FFmpeg.Runtime;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableScaleFilters
{
    public static IPipelineFilterStep ForAcceleration(
        IRuntimeInfo runtimeInfo,
        HardwareAccelerationMode accelMode,
        FrameState currentState,
        FrameSize scaledSize,
        FrameSize paddedSize) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new ScaleCudaFilter(currentState, scaledSize, paddedSize),
            HardwareAccelerationMode.Qsv when currentState.FrameDataLocation == FrameDataLocation.Hardware ||
                                              scaledSize == paddedSize =>
                new ScaleQsvFilter(runtimeInfo, currentState, scaledSize, paddedSize),
            HardwareAccelerationMode.Vaapi => new ScaleVaapiFilter(currentState, scaledSize, paddedSize),
            _ => new ScaleFilter(currentState, scaledSize, paddedSize)
        };
}
