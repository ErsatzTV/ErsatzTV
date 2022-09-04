using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableSubtitleScaleFilters
{
    public static IPipelineFilterStep ForAcceleration(
        HardwareAccelerationMode accelMode,
        FrameState currentState,
        FrameSize scaledSize,
        FrameSize paddedSize,
        int extraHardwareFrames) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new SubtitleScaleNppFilter(currentState, scaledSize, paddedSize),
            HardwareAccelerationMode.Qsv => new SubtitleScaleQsvFilter(
                currentState,
                scaledSize,
                paddedSize,
                extraHardwareFrames),
            _ => new ScaleImageFilter(paddedSize)
        };
}
