using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;
using ErsatzTV.FFmpeg.Filter.Vaapi;
using ErsatzTV.FFmpeg.Runtime;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableSubtitleScaleFilters
{
    public static IPipelineFilterStep ForAcceleration(
        IRuntimeInfo runtimeInfo,
        HardwareAccelerationMode accelMode,
        FrameState currentState,
        FrameSize scaledSize,
        FrameSize paddedSize) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new SubtitleScaleNppFilter(currentState, scaledSize, paddedSize),
            HardwareAccelerationMode.Qsv when currentState.FrameDataLocation == FrameDataLocation.Hardware ||
                                              scaledSize == paddedSize =>
                new ScaleQsvFilter(runtimeInfo, currentState, scaledSize, paddedSize),
            _ => new ScaleImageFilter(paddedSize)
        };
}
