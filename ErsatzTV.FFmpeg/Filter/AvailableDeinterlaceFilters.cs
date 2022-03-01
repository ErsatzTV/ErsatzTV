using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Vaapi;
using LanguageExt;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableDeinterlaceFilters
{
    public static IPipelineFilterStep ForAcceleration(
        HardwareAccelerationMode accelMode,
        FrameState currentState,
        FrameState desiredState,
        Option<WatermarkInputFile> watermarkInputFile) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc => new YadifCudaFilter(currentState),

            // deinterlace_qsv seems to create timestamp issues
            // HardwareAccelerationMode.Qsv => new DeinterlaceQsvFilter(currentState),

            // fall back to software deinterlace with watermark and no scaling
            HardwareAccelerationMode.Vaapi when watermarkInputFile.IsNone ||
                                                (currentState.ScaledSize != desiredState.ScaledSize) =>
                new DeinterlaceVaapiFilter(currentState),

            _ => new YadifFilter(currentState)
        };
}
