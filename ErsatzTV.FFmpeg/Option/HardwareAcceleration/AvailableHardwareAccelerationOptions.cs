namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public static class AvailableHardwareAccelerationOptions
{
    public static IPipelineStep ForMode(HardwareAccelerationMode mode)
    {
        return mode switch
        {
            HardwareAccelerationMode.Nvenc => new CudaHardwareAccelerationOption(),
            HardwareAccelerationMode.Qsv => new QsvHardwareAccelerationOption(),
            HardwareAccelerationMode.Vaapi => new VaapiHardwareAccelerationOption(),
            HardwareAccelerationMode.VideoToolbox => new VideoToolboxHardwareAccelerationOption(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}
