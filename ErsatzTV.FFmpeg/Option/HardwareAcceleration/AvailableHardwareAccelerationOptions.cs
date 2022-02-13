namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public static class AvailableHardwareAccelerationOptions
{
    public static IPipelineStep ForMode(HardwareAccelerationMode mode)
    {
        return mode switch
        {
            HardwareAccelerationMode.Nvenc => new CudaHardwareAccelerationOption(),
            HardwareAccelerationMode.Qsv => new QsvHardwareAccelerationOption(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}
