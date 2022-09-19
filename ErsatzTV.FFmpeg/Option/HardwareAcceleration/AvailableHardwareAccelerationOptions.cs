using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public static class AvailableHardwareAccelerationOptions
{
    public static Option<IPipelineStep> ForMode(
        HardwareAccelerationMode mode,
        Option<string> gpuDevice,
        ILogger logger) =>
        mode switch
        {
            HardwareAccelerationMode.Nvenc => new CudaHardwareAccelerationOption(),
            HardwareAccelerationMode.Qsv => new QsvHardwareAccelerationOption(gpuDevice),
            HardwareAccelerationMode.Vaapi => GetVaapiAcceleration(gpuDevice, logger),
            HardwareAccelerationMode.VideoToolbox => new VideoToolboxHardwareAccelerationOption(),
            HardwareAccelerationMode.Amf => new AmfHardwareAccelerationOption(),
            HardwareAccelerationMode.None => Option<IPipelineStep>.None,
            _ => LogUnknownMode(mode, logger)
        };

    private static Option<IPipelineStep> GetVaapiAcceleration(Option<string> vaapiDevice, ILogger logger)
    {
        foreach (string device in vaapiDevice)
        {
            return new VaapiHardwareAccelerationOption(device);
        }

        logger.LogWarning("VAAPI device name is missing; falling back to software mode");
        return Option<IPipelineStep>.None;
    }

    private static Option<IPipelineStep> LogUnknownMode(HardwareAccelerationMode mode, ILogger logger)
    {
        logger.LogWarning("Unexpected hardware acceleration mode {AccelMode}; may have playback issues", mode);
        return Option<IPipelineStep>.None;
    }
}
