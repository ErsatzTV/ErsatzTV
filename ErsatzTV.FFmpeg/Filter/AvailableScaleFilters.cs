using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;
using ErsatzTV.FFmpeg.Filter.Vaapi;
using ErsatzTV.FFmpeg.Runtime;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableScaleFilters
{
    public static IPipelineFilterStep ForAcceleration(
        IRuntimeInfo _,
        HardwareAccelerationMode accelMode,
        FrameState currentState,
        FrameSize scaledSize,
        FrameSize paddedSize,
        int extraHardwareFrames,
        bool isAnamorphicEdgeCase,
        string sampleAspectRatio) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc =>
                new ScaleCudaFilter(currentState, scaledSize, paddedSize, isAnamorphicEdgeCase),
            HardwareAccelerationMode.Qsv when currentState.FrameDataLocation == FrameDataLocation.Hardware ||
                                              scaledSize == paddedSize =>
                new ScaleQsvFilter(
                    currentState,
                    scaledSize,
                    extraHardwareFrames,
                    isAnamorphicEdgeCase,
                    sampleAspectRatio),
            HardwareAccelerationMode.Vaapi => new ScaleVaapiFilter(
                currentState,
                scaledSize,
                paddedSize,
                isAnamorphicEdgeCase),
            _ => new ScaleFilter(currentState, scaledSize, paddedSize, isAnamorphicEdgeCase)
        };
}
