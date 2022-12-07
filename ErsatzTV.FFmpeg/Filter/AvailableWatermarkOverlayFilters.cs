using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Filter;

public static class AvailableWatermarkOverlayFilters
{
    public static IPipelineFilterStep ForAcceleration(
        HardwareAccelerationMode accelMode,
        WatermarkState watermarkState,
        FrameSize resolution,
        FrameSize squarePixelFrameSize,
        ILogger logger) =>
        accelMode switch
        {
            HardwareAccelerationMode.Nvenc =>
                new OverlayWatermarkCudaFilter(watermarkState, resolution, squarePixelFrameSize, logger),
            HardwareAccelerationMode.Qsv =>
                new OverlayWatermarkQsvFilter(watermarkState, resolution, squarePixelFrameSize, logger),
            _ => new OverlayWatermarkFilter(
                watermarkState,
                resolution,
                squarePixelFrameSize,
                new PixelFormatYuv420P(),
                logger)
        };
}
