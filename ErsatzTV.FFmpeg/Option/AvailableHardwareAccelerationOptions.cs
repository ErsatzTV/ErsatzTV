namespace ErsatzTV.FFmpeg.Option;

public static class AvailableHardwareAccelerationOptions
{
    public static IPipelineStep ForMode(HardwareAccelerationMode mode)
    {
        return mode switch
        {
            HardwareAccelerationMode.Nvenc => new CudaHardwareAccelerationOption(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}
