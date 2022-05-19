using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Vaapi;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableDeinterlaceFilters
{
    public static IPipelineFilterStep ForAcceleration(
        HardwareAccelerationMode accelMode,
        FrameState currentState,
        FrameState desiredState,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new YadifCudaFilter(currentState),

            // deinterlace_qsv seems to create timestamp issues
            // HardwareAccelerationMode.Qsv => new DeinterlaceQsvFilter(currentState),

            // fall back to software deinterlace with watermark and no scaling
            HardwareAccelerationMode.Vaapi when watermarkInputFile.IsNone && subtitleInputFile.IsNone ||
                                                currentState.ScaledSize != desiredState.ScaledSize =>
                new DeinterlaceVaapiFilter(currentState),

            _ => new YadifFilter(currentState)
        };
}
